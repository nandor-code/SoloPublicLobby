using NetFwTypeLib;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Solo_Public_Lobby.Helpers
{
    public class FirewallRule
    {
        public static Label lblAdmin = null;

        private enum RuleProtocol
        {
            eRuleProtoTCP = 6,
            eRuleProtoUDP = 17
        }

        /// <summary>
        /// Sets, Removes or Toggles CodeSwine Outbound firewall rules.
        /// </summary>
        /// <param name="addresses">Scope to block.</param>
        /// <param name="enabled">True to enable, false to disable the rule.</param>
        /// <param name="toggle">True to prevent adding or removing the rule again.</param>
        public static bool CreateOutbound(string addresses, Game game, bool enabled, bool toggle)
        {
            try
            {
                INetFwRule firewallRuleTCP = createFWRule(addresses, game, enabled, true, RuleProtocol.eRuleProtoTCP);
                INetFwRule firewallRuleUDP = createFWRule(addresses, game, enabled, true, RuleProtocol.eRuleProtoUDP);

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                if (!toggle)
                {
                    if( game.tcpPorts.Length > 0 )
                        firewallPolicy.Rules.Add(firewallRuleTCP);
                    if (game.udpPorts.Length > 0)
                        firewallPolicy.Rules.Add(firewallRuleUDP);
                }
                else
                {
                    if (game.tcpPorts.Length > 0)
                    {
                        firewallPolicy.Rules.Remove(firewallRuleTCP.Name);
                        firewallPolicy.Rules.Add(firewallRuleTCP);
                    }
                    if (game.udpPorts.Length > 0)
                    {
                        firewallPolicy.Rules.Remove(firewallRuleUDP.Name);
                        firewallPolicy.Rules.Add(firewallRuleUDP);
                    }
                }

            } catch (Exception e)
            {
                ErrorLogger.LogException(e);
                if (lblAdmin != null)
                    lblAdmin.Visibility = Visibility.Visible;
                else
                    MessageBox.Show("Please start this program as administrator!");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets, Removes or Toggles CodeSwine Inbound firewall rules.
        /// </summary>
        /// <param name="addresses">Scope to block.</param>
        /// <param name="enabled">True to enable, false to disable the rule.</param>
        /// <param name="toggle">True to prevent adding or removing the rule again.</param>
        public static bool CreateInbound(string addresses, Game game, bool enabled, bool toggle)
        {
            try
            {
                INetFwRule firewallRuleTCP = createFWRule(addresses, game, enabled, false, RuleProtocol.eRuleProtoTCP);
                INetFwRule firewallRuleUDP = createFWRule(addresses, game, enabled, false, RuleProtocol.eRuleProtoUDP);

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                if(!toggle)
                {
                    if (game.tcpPorts.Length > 0)
                        firewallPolicy.Rules.Add(firewallRuleTCP);
                    if (game.udpPorts.Length > 0)
                        firewallPolicy.Rules.Add(firewallRuleUDP);
                } else
                {
                    if (game.tcpPorts.Length > 0)
                    {
                        firewallPolicy.Rules.Remove(firewallRuleTCP.Name);
                        firewallPolicy.Rules.Add(firewallRuleTCP);
                    }
                    if (game.udpPorts.Length > 0)
                    {
                        firewallPolicy.Rules.Remove(firewallRuleUDP.Name);
                        firewallPolicy.Rules.Add(firewallRuleUDP);
                    }

                }

            }
            catch (Exception e)
            {
                ErrorLogger.LogException(e);
                return false;
            }

            return true;
        }

        private static INetFwRule createFWRule(string addresses, Game game, bool enabled, bool bOutbound, RuleProtocol proto )
        {
            INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));

            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            firewallRule.Protocol = (int)proto;
            firewallRule.Enabled = enabled;
            firewallRule.InterfaceTypes = "All";
            if (!string.IsNullOrEmpty(addresses))
            {
                firewallRule.RemoteAddresses = addresses;
            }

            Console.WriteLine(addresses);

            firewallRule.LocalPorts = proto == RuleProtocol.eRuleProtoTCP ? game.tcpPorts : game.udpPorts;
            firewallRule.Name = proto == RuleProtocol.eRuleProtoTCP ? game.GetTCPRuleName(bOutbound ? "Outbound" : "Inbound") : game.GetUDPRuleName(bOutbound ? "Outbound" : "Inbound");
            firewallRule.Direction = bOutbound ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;

            return firewallRule;
        }

        /// <summary>
        /// Removes CodeSwine Inbound & Outbound firewall rules at program startup.
        /// </summary>
        public static void DeleteRules( Game game )
        {
            try {
                INetFwRule firewallRuleInboundTCP = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRuleInboundTCP.Name = game.GetTCPRuleName("Inbound");
                
                INetFwRule firewallRuleInboundUDP = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRuleInboundUDP.Name = game.GetUDPRuleName("Inbound");

                INetFwRule firewallRuleOutboundTCP = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRuleOutboundTCP.Name = game.GetTCPRuleName("Outbound");

                INetFwRule firewallRuleOutboundUDP = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
                firewallRuleOutboundUDP.Name = game.GetUDPRuleName("Outbound");


                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

                if (game.tcpPorts.Length > 0)
                {
                    firewallPolicy.Rules.Remove(firewallRuleInboundTCP.Name);
                    firewallPolicy.Rules.Remove(firewallRuleOutboundTCP.Name);
                }

                if (game.udpPorts.Length > 0)
                {
                    firewallPolicy.Rules.Remove(firewallRuleInboundUDP.Name);
                    firewallPolicy.Rules.Remove(firewallRuleOutboundUDP.Name);
                }
            } catch (Exception e)
            {
                ErrorLogger.LogException(e);
                if (lblAdmin != null)
                    lblAdmin.Visibility = Visibility.Visible;
                else
                    MessageBox.Show("Run this program as administrator!");
            }
        }
    }
}   