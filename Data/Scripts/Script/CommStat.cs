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

namespace CommStat
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CommStat : MySessionComponentBase
    {

        ushort netid = 11593;

        internal bool client;

        public override void BeforeStart()
        {

            var IsServer = MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Session.IsServer;
            var MpActive = MyAPIGateway.Multiplayer.MultiplayerActive;
            var DedicatedServer = MyAPIGateway.Utilities.IsDedicated;
            var IsClient = !IsServer && !DedicatedServer && MpActive;   
            var IsHost = IsServer && !DedicatedServer && MpActive;
            client = IsHost || IsClient || !MpActive;//if player is host, client, or offline bot,
            if (client)
            {
                MyAPIGateway.Utilities.ShowMessage("AWG", "Welcome to my mods, '/tank' to see recent mod updates and commands!");
                MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEnteredSender;

            }
        }

        private void OnMessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            var lowerMessage = messageText.ToLower();//messageText is user inputted, lowerMessage is the variable of the one converted to lower case

            if (lowerMessage == "/tank")
            {
                string title = "AWG Mods";

                string message =

                    "Welcome to the CWP, the navigation commands are listed below. \n\n/Tips\n/FAQ\n/Updates"
                    

                    ;

                string closebutton =
                    "Close"
                    ;
                //Divide integ by 8 to get mm, steel is weak to HEAT, ammo vels, so on

                MyAPIGateway.Utilities.ShowMissionScreen(
                    title, "", "", message, null, closebutton);
                sendToOthers = false;
                return;
            }

            if (lowerMessage == "/tips")
            {
                string title = "Tips, Stats, Notes";

                string message =

                    "Steel in the CWP is closer to IRL aluminium.\nDivide in-game integrity values by 8 to get\nthe amount in RHA mm." +
                    "There are 5 main steel strengths - 4/12/25/50/100mm.\n" +
                    "Muzzle velocity is generally linked to ammo type. The following is in m/s.\n AP/APHE: 500 \n APDS: 750 \n APFSDS: 1500 \n HE/HEAT/HESH/Smoke: 400 \n Class 0/165mm/Low Prop-Charge Ammo: 200 \n " +
                    "More to come in future, these is just the early stages."

                    ;

                string closebutton =
                    "Close"
                    ;
                //Divide integ by 8 to get mm, steel is weak to HEAT, ammo vels, so on

                MyAPIGateway.Utilities.ShowMissionScreen(
                    title, "", "", message, null, closebutton);
                sendToOthers = false;
                return;
            }

            if (lowerMessage == "/faq")
            {
                string title = "Frequently Asked Questions";

                string message =

                    "To be added to." +
                    ""


                    ;

                string closebutton =
                    "Close"
                    ;
                //Divide integ by 8 to get mm, steel is weak to HEAT, ammo vels, so on

                MyAPIGateway.Utilities.ShowMissionScreen(
                    title, "", "", message, null, closebutton);
                sendToOthers = false;
                return;
            }

            if (lowerMessage == "/about")
            {
                string title = "About the CWP and AWG's Credits";

                string message =

                    "Going on 5 years now, this ongoing project has become more than long-term, so some parts will be more up to date than others. " +
                    "Nevertheless, I love working on this, and it's been a largely one-woman project for that time, with help from various " +
                    "lovely people along the way, such as Whiplash141, Klime, Zomb3yKiller, Randaped, BDCarillo, and more.\n\n " +
                    "It was initially made to supplement the Space Engineers Tank Battle server, by reskinning hydrogen thrusters, and vents into " +
                    "C-1 composite and 'ERA', long since renamed to NERA, you can still find both used in various tanks today. It grew from there, \n" +
                    "with reactor tank engines being supplanted by hydrogen, ammunition explosions sliding into play around early 2020, and evermore \n" +
                    "planned and in the works going forwards.\n\n " +
                    "Though I am not active on the Workshop or many Discord communities anymore, I do appreciate everyone that's helped me get here, or \n " +
                    "even just clicked subscribe, you've helped me learn more than I ever thought I would, meet people I never expected, and kept me \n" +
                    "distracted from my long term cancer as long as I've needed."


                    ;

                string closebutton =
                    "Thank you all"
                    ;
                //Divide integ by 8 to get mm, steel is weak to HEAT, ammo vels, so on

                MyAPIGateway.Utilities.ShowMissionScreen(
                    title, "", "", message, null, closebutton);
                sendToOthers = false;
                return;
            }

            return; //You must have this return or ALL player messages in chat disappear  
        }

        protected override void UnloadData()
        {
            if (client)
            {
                MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEnteredSender;
            }
        }
    }
}

