using System;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRageMath;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using ProtoBuf;

namespace MagazineExplosion
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MagazineExplosion : MySessionComponentBase
    {
        // **CONFIG **
        Dictionary<string, float> lookup_dict = new Dictionary<string, float>() // Valid inventory items to trigger an explosion. Numbers correspond to multipliers. Default: 1)
        {
            {"Shell",1},
            {"Magazine",1},
            {"Ammo",1},
            {"Explosives",1},
            {"Fuel",1},
            {"MFiveC",1},
            {"Drum",1},
            {"RefinedPlatonite",10},
            {"Mortar600mmAmmoMagazine",7f},
        };

        float cargo_integrity_trigger = 0.5f; // How damaged the cargo must be before it explodes. Eg: 0.5f = 50% damage. Default: 0.5f

        float minimum_explosive_power = 1f; // Minimum base damage for all explosions. Default: 200f
        float maximum_explosive_power = 5000f; // Maximum base damage for all explosions. Default: 3000f;
        float small_boom = 500f; // Any explosive power under this will count as "small boom". Default: 500f;
        float medium_boom = 1500f; // Any explosive power under this will count as "medium boom". Default: 1500f; Anything greater than this will be a 'big boom'
        float big_boom = 3000f; // Any explosive power under this will count as "big boom". Default: 3000f;
        float large_grid_explosive_multiplier = 1.2f; // Multiplies the base damage for large grids. Default: 2.5f;
        float small_grid_explosive_multiplier = 1.2f; // Multiplies the base damage for small grids. Default: 2.5f;

        float minimum_radius = 0.05f; // Minimum radius of the explosion (meters). Default: 1f;
        float maximum_radius = 200f; // Maximum radius of the explosion (meters). Default: 200f;
        float explosive_radius_ratio = 150f; // Multiplies the explosive power by 1/ratio to get radius. Eg: Explosive Power = 2000, Radius = 10. Default: 200f;

        int minimum_delay = 240; // Minimum delay between the cookoff and the explosion. Time in ticks (60 ticks per second). Default: 120
        int maximum_delay = 480; // Maximum delay between the cookoff and the explosion. Time in ticks (60 ticks per second). Default: 480

        string cookoff_particle = "CookOffParticleRedone"; // Particle that plays before a cargo explodes. Default: CookOffParticle;
        float cookoff_particle_smallgrid_scale = 0.8f; // Particle scale for small grids. Default: 0.5f;

        string explosion_particle = "AmmoExplosionMedium"; // Particle that plays when the explosion is triggered. Default: ;
        float explosion_particle_smallgrid_scale = 0.75f; // Particle scale for small grids. Default: 0.5f;

        // **CORE**
        Dictionary<long, DelayedExplosion> all_explosions = new Dictionary<long, DelayedExplosion>();
        List<long> rem_explosions = new List<long>();
        List<ShortParticle> all_particles = new List<ShortParticle>();
        List<ShortParticle> rem_particles = new List<ShortParticle>();

        List<MyInventoryItem> all_items = new List<MyInventoryItem>();
        List<IMyPlayer> all_players = new List<IMyPlayer>();
        DelayedExplosion reuse_delayed_explosion;
        MatrixD particle_wm = MatrixD.Identity;

        ushort netid = 29943;
        int timer = 0;

        [ProtoContract]
        public class ShortParticle
        {
            [ProtoMember(1)]
            public long grid_entity_id;
            [ProtoMember(2)]
            public Vector3I grid_pos;
            [ProtoMember(3)]
            public int end_time;
            [ProtoMember(4)]
            public string particle_name;
            [ProtoMember(5)]
            public long block_entity_id;

            [ProtoIgnore]
            public MyParticleEffect effect;
            [ProtoIgnore]
            public VRage.Game.ModAPI.IMyCubeBlock block;

            public ShortParticle()
            {

            }

            public ShortParticle(long grid_entity_id,Vector3I grid_pos, int end_time,string particle_name, long block_entity_id)
            {
                this.grid_entity_id = grid_entity_id;
                this.grid_pos = grid_pos;
                this.end_time = end_time;
                this.particle_name = particle_name;
                this.block_entity_id = block_entity_id;
            }

        }

        public class DelayedExplosion
        {
            public VRage.Game.ModAPI.IMyCubeBlock block;
            public float explosive_damage;
            public float radius;
            public int trigger_time;
            public VRage.Game.ModAPI.IMyCubeGrid grid;
            public Vector3I grid_pos;
            public MatrixD woldmatrix_cache;

            public DelayedExplosion(VRage.Game.ModAPI.IMyCubeBlock block, float explosive_damage, float radius, int trigger_time, VRage.Game.ModAPI.IMyCubeGrid grid,
                Vector3I grid_pos)
            {
                this.block = block;
                this.explosive_damage = explosive_damage;
                this.radius = radius;
                this.trigger_time = trigger_time;
                this.grid = grid;
                this.grid_pos = grid_pos;
                this.woldmatrix_cache = block.WorldMatrix;
            }

        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, ProcessDamage);
            }
            MyAPIGateway.Multiplayer.RegisterMessageHandler(netid, ParticleHandler);
        }

        public void ProcessDamage(object target, ref MyDamageInformation info)
        {
            try
            {
                if (target is VRage.Game.ModAPI.IMySlimBlock)
                {
                    VRage.Game.ModAPI.IMySlimBlock slim = target as VRage.Game.ModAPI.IMySlimBlock;
                    if (slim.FatBlock != null && info.Type != MyDamageType.Grind && (slim.Integrity - info.Amount) <= (slim.MaxIntegrity * cargo_integrity_trigger)
                        && !all_explosions.ContainsKey(slim.FatBlock.EntityId) && slim.FatBlock.HasInventory)
                    {
                        VRage.Game.ModAPI.IMyInventory inventory = slim.FatBlock.GetInventory();
                        if (inventory != null && !inventory.Empty())
                        {
                            bool valid_inventory = false;
                            all_items.Clear();
                            inventory.GetItems(all_items);
                            float total_explosive_power = 0;

                            foreach (var item in all_items)
                            {
                                string key = "";

                                foreach (var validkey in lookup_dict.Keys)
                                {
                                    if (item.Type.SubtypeId.Contains(validkey))
                                    {
                                        key = validkey;
                                        break;
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(key))
                                {
                                    if (item.Amount > 0)
                                    {
                                        total_explosive_power += item.Amount.ToIntSafe() * item.Type.GetItemInfo().Mass * lookup_dict[key];
                                        valid_inventory = true;
                                        if (item.Type.SubtypeId == "RefinedPlatonite")
                                        {
                                            cookoff_particle = "CookOffParticleReactor";
                                            explosion_particle = "ReactorExplosion";
                                        }
                                    }
                                }

                            }

                            if (valid_inventory)
                            {
                                total_explosive_power = MyMath.Clamp(total_explosive_power, minimum_explosive_power, maximum_explosive_power);

                                if (slim.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                                {
                                    total_explosive_power *= large_grid_explosive_multiplier;
                                }
                                else
                                {
                                    total_explosive_power *= small_grid_explosive_multiplier;
                                }

                                int random_end_time = MyUtils.GetRandomInt(minimum_delay, maximum_delay);

                                float radius = MyMath.Clamp(total_explosive_power / explosive_radius_ratio, minimum_radius, maximum_radius);
                                ShortParticle short_particle = new ShortParticle(slim.CubeGrid.EntityId, slim.Position, timer + random_end_time, cookoff_particle, slim.FatBlock.EntityId);
                                DelayedExplosion delayed_explosion = new DelayedExplosion(slim.FatBlock, total_explosive_power, radius, timer + random_end_time, slim.CubeGrid, slim.Position);
                                if (delayed_explosion != null && !all_explosions.ContainsKey(slim.FatBlock.EntityId))
                                {
                                    all_explosions.Add(slim.FatBlock.EntityId, delayed_explosion);
                                }

                                if (short_particle != null)
                                {
                                    all_players.Clear();
                                    MyAPIGateway.Multiplayer.Players.GetPlayers(all_players);
                                    foreach (var p in all_players)
                                    {
                                        MyAPIGateway.Multiplayer.SendMessageTo(netid, MyAPIGateway.Utilities.SerializeToBinary(short_particle), p.SteamUserId);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void ParticleHandler(byte[] obj) // Network handler for clients
        {
            try
            {
                if (MyAPIGateway.Utilities.IsDedicated)
                {
                    return;
                }

                ShortParticle short_particle = MyAPIGateway.Utilities.SerializeFromBinary<ShortParticle>(obj);
                if (short_particle != null)
                {
                    VRage.Game.ModAPI.IMyCubeBlock block = MyAPIGateway.Entities.GetEntityById(short_particle.block_entity_id) as VRage.Game.ModAPI.IMyCubeBlock;

                    if (block != null)
                    {
                        short_particle.block = block;

                        Vector3D local_pos = short_particle.grid_pos;
                        Vector3D world_pos = block.WorldMatrix.Translation;

                        MatrixD particle_mat = block.WorldMatrix;

                        MyParticlesManager.TryCreateParticleEffect(short_particle.particle_name, ref particle_mat, ref world_pos, uint.MaxValue, out short_particle.effect);
                        if (short_particle.effect != null)
                        {
                            float scale = 1f;
                            if (short_particle.block.CubeGrid.GridSizeEnum == MyCubeSize.Small) // Small grid cookoff particle scaling
                            {
                                scale *= cookoff_particle_smallgrid_scale;
                            }
                            short_particle.effect.UserScale = scale;
                            all_particles.Add(short_particle);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    rem_particles.Clear();
                    foreach (var particle in all_particles)
                    {
                        if (particle.block.MarkedForClose)
                        {
                            rem_particles.Add(particle);
                        }

                        else
                        {
                            if (particle.effect != null)
                            {
                                // Ensures particle always fires in the direction opposite gravity
                                float grav_inter = 0;
                                Vector3D grav = -1 * Vector3D.Normalize(MyAPIGateway.Physics.CalculateNaturalGravityAt(particle.block.WorldMatrix.Translation, out grav_inter));
                                if (grav_inter == 0)
                                {
                                    grav = particle.block.WorldMatrix.Up; 
                                }

                                particle_wm = MatrixD.CreateWorld(particle.block.WorldMatrix.Translation, grav, MyUtils.GetRandomPerpendicularVector(ref grav));
                                particle.effect.WorldMatrix = particle_wm;
                            }
                        }

                        if (timer >= particle.end_time)
                        {
                            rem_particles.Add(particle);
                        }
                    }

                    foreach (var rem_particle in rem_particles)
                    {
                        rem_particle.effect.Stop(false);
                        rem_particle.effect.StopEmitting();
                        rem_particle.effect.StopLights();
                        rem_particle.effect = null;
                        all_particles.Remove(rem_particle);
                    }
                }

                if (MyAPIGateway.Session.IsServer)
                {
                    rem_explosions.Clear();
                    foreach (var block_id in all_explosions.Keys)
                    {
                        reuse_delayed_explosion = all_explosions[block_id];

                        if (!reuse_delayed_explosion.block.MarkedForClose) // Using a cache to avoid resetting the WorldMatrix
                        {
                            reuse_delayed_explosion.woldmatrix_cache = reuse_delayed_explosion.block.WorldMatrix;
                        }

                        bool valid_explosion = false;
                        if (!reuse_delayed_explosion.block.MarkedForClose) // Triggers the explosion immediately if the block is fully destroyed
                        {
                            if (timer == reuse_delayed_explosion.trigger_time)
                            {
                                valid_explosion = true;
                            }
                        }
                        else
                        {
                            if (timer <= reuse_delayed_explosion.trigger_time)
                            {
                                valid_explosion = true;
                                Vector3D position = Vector3D.Transform((Vector3D)reuse_delayed_explosion.grid_pos, reuse_delayed_explosion.grid.WorldMatrix);
                                MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("Ammo_Sparks", position);
                            }
                        }

                        if (valid_explosion)
                        {
                            Vector3D location = reuse_delayed_explosion.woldmatrix_cache.Translation;
                            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("AmmoRackFire", location);
                            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("Ammo_Sparks", location);
                            // Conditional explosions. Each category supports multiple explosions
                            if (reuse_delayed_explosion.explosive_damage <= small_boom)
                            {
                                // **SMALL BOOM 1**
                                BoundingSphereD sphere_1 = new BoundingSphereD(location, reuse_delayed_explosion.radius);
                                MyExplosionInfo explosion_1 = new MyExplosionInfo(150f, reuse_delayed_explosion.explosive_damage, sphere_1, MyExplosionTypeEnum.WARHEAD_EXPLOSION_02, true);

                                explosion_1.PlayerDamage = 70f;
                                explosion_1.ExplosionSphere = sphere_1; //Center + Radius
                                explosion_1.Damage = reuse_delayed_explosion.explosive_damage;
                                explosion_1.CustomEffect = "Explosion_Warhead_02";
                                explosion_1.ParticleScale *= MyMath.Clamp(reuse_delayed_explosion.radius, 1, 5);
                                explosion_1.AffectVoxels = false;
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Large) //Large grid impulse scaling
                                {
                                    explosion_1.StrengthImpulse = 1;
                                }
                                else
                                {
                                    explosion_1.StrengthImpulse = 6;
                                }
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Small) //Small grid particle scaling
                                {
                                    explosion_1.ParticleScale *= explosion_particle_smallgrid_scale;
                                }
                                MyExplosions.AddExplosion(ref explosion_1);
                                // **END SMALL BOOM 1**




                                // **SMALL BOOM 2**
                                BoundingSphereD sphere_5 = new BoundingSphereD(location, reuse_delayed_explosion.radius);
                                MyExplosionInfo explosion_5 = new MyExplosionInfo(150f, reuse_delayed_explosion.explosive_damage, sphere_5, MyExplosionTypeEnum.WARHEAD_EXPLOSION_02, true);

                                explosion_5.PlayerDamage = 1f;
                                explosion_5.ExplosionSphere = sphere_5; //Center + Radius
                                explosion_5.Damage = 1f;
                                explosion_5.CustomEffect = "Explosion_Warhead_02";
                                explosion_5.ParticleScale *= MyMath.Clamp(reuse_delayed_explosion.radius, 1, 5);
                                explosion_5.AffectVoxels = false;
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Large) //Large grid impulse scaling
                                {
                                    explosion_5.StrengthImpulse = 1;
                                }
                                else
                                {
                                    explosion_5.StrengthImpulse = 6;
                                }
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Small) //Small grid particle scaling
                                {
                                    explosion_5.ParticleScale *= explosion_particle_smallgrid_scale;
                                }
                                MyExplosions.AddExplosion(ref explosion_5);
                                // **END SMALL BOOM 2**
                            }



                            else if (reuse_delayed_explosion.explosive_damage <= medium_boom)
                            {
                                // **MEDIUM BOOM**
                                BoundingSphereD sphere_2 = new BoundingSphereD(location, reuse_delayed_explosion.radius);
                                MyExplosionInfo explosion_2 = new MyExplosionInfo(150f, reuse_delayed_explosion.explosive_damage, sphere_2, MyExplosionTypeEnum.WARHEAD_EXPLOSION_02, true);

                                explosion_2.PlayerDamage = 99f;
                                explosion_2.ExplosionSphere = sphere_2; //Center + Radius
                                explosion_2.Damage = reuse_delayed_explosion.explosive_damage;
                                explosion_2.CustomEffect = "AmmoExplosionMedium";
                                explosion_2.ParticleScale *= MyMath.Clamp(reuse_delayed_explosion.radius, 1, 5);
                                explosion_2.AffectVoxels = false;
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Large) //Large grid impulse scaling
                                {
                                    explosion_2.StrengthImpulse = 1;
                                }
                                else
                                {
                                    explosion_2.StrengthImpulse = 4;
                                }
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Small) //Small grid particle scaling
                                {
                                    explosion_2.ParticleScale *= explosion_particle_smallgrid_scale;
                                }
                                MyExplosions.AddExplosion(ref explosion_2);
                                // **END MEDIUM BOOM**
                            }



                            else if (reuse_delayed_explosion.explosive_damage > medium_boom)
                            {
                                // **BIG BOOM**
                                BoundingSphereD sphere_3 = new BoundingSphereD(location, reuse_delayed_explosion.radius);
                                MyExplosionInfo explosion_3 = new MyExplosionInfo(150f, reuse_delayed_explosion.explosive_damage, sphere_3, MyExplosionTypeEnum.WARHEAD_EXPLOSION_02, true);

                                explosion_3.PlayerDamage = 150f;
                                explosion_3.ExplosionSphere = sphere_3; //Center + Radius
                                explosion_3.Damage = reuse_delayed_explosion.explosive_damage;
                                explosion_3.CustomEffect = "Explosion_Warhead_50";
                                explosion_3.ParticleScale *= MyMath.Clamp(reuse_delayed_explosion.radius, 1, 5);
                                explosion_3.AffectVoxels = false;
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Large) //Large grid impulse scaling
                                {
                                    explosion_3.StrengthImpulse = 1;
                                }
                                else
                                {
                                    explosion_3.StrengthImpulse = 3;
                                }
                                if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Small) //Small grid particle scaling
                                {
                                    explosion_3.ParticleScale *= explosion_particle_smallgrid_scale;
                                }
                                MyExplosions.AddExplosion(ref explosion_3);
                                // **END BIG BOOM**
                            }

                            // **POST BOOM**
                            BoundingSphereD sphere_4 = new BoundingSphereD(location, reuse_delayed_explosion.radius);
                            MyExplosionInfo explosion_4 = new MyExplosionInfo(150f, reuse_delayed_explosion.explosive_damage, sphere_4, MyExplosionTypeEnum.WARHEAD_EXPLOSION_02, true);

                            explosion_4.PlayerDamage = 75f;
                            explosion_4.ExplosionSphere = sphere_4; //Center + Radius
                            explosion_4.Damage = 25f;
                            explosion_4.CustomEffect = "Explosion_Warhead_02";
                            explosion_4.ParticleScale *= MyMath.Clamp(reuse_delayed_explosion.radius, 1, 5);
                            explosion_4.AffectVoxels = false;

                            if (reuse_delayed_explosion.block.CubeGrid.GridSizeEnum == MyCubeSize.Large) //Large grid impulse scaling
                            {
                                explosion_4.StrengthImpulse = 1;
                            }
                            else
                            {
                                explosion_4.StrengthImpulse = 5;
                            }
                            
                            MyExplosions.AddExplosion(ref explosion_4);
                            //MyVisualScriptLogicProvider.SendChatMessage("Damage: " + explosion_4.Damage + "\nRadius: " + explosion_4.ExplosionSphere.Radius + "\nImpulse: " + explosion_4.StrengthImpulse);
                            // **END POST BOOM**

                            reuse_delayed_explosion.block.CubeGrid.RazeBlock(reuse_delayed_explosion.block.Position); //Delete the cargo to prevent double explosions
                            rem_explosions.Add(block_id); // Cleanup
                        }
                    }

                    foreach (var rem_block_id in rem_explosions)
                    {
                        if (all_explosions.ContainsKey(rem_block_id))
                        {
                            all_explosions.Remove(rem_block_id);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            timer += 1;
        }


        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(netid, ParticleHandler);
        }
    }
}

