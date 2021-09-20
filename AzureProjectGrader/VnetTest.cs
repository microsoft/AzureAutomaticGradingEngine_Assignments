using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using NUnit.Framework;
using System;
using System.Linq;

namespace AzureProjectGrader
{
    public class VnetTests
    {
        private NetworkManagementClient client;
        private VirtualNetwork vnet1, vnet2;

        [SetUp]
        public void Setup()
        {
            var config = new Config();
            client = new NetworkManagementClient(config.Credentials);
            client.SubscriptionId = config.SubscriptionId;
            vnet1 = client.VirtualNetworks.Get(Constants.ResourceGroupName, Constants.Vnet1Name);
            vnet2 = client.VirtualNetworks.Get(Constants.ResourceGroupName, Constants.Vnet2Name);
        }

        private Subnet GetVnet1PublicSubnet()
        {
            return vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.1.0/24");
        }


        private Subnet GetVnet2PublicSubnet()
        {
            return vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.1.0/24");
        }

        private Subnet GetVnet1PrivateSubnet()
        {
            return vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/24");
        }

        private Subnet GetVnet2PrivateSubnet()
        {
            return vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/24");
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
        }

        [Test]
        public void Test01_Have2VnetsIn2Regions()
        {
            Assert.DoesNotThrow(() => { Console.WriteLine(vnet1.Name); });
            Assert.AreEqual("southeastasia", vnet1.Location);
            Assert.DoesNotThrow(() => { Console.WriteLine(vnet2.Name); });
            Assert.AreEqual("eastasia", vnet2.Location);
        }

        [Test]
        public void Test02_VnetAddressSpace()
        {
            Assert.AreEqual("10.0.0.0/16", vnet1.AddressSpace.AddressPrefixes[0], "Vnet1 Address space 10.0.0.0/16");
            Assert.AreEqual("10.1.0.0/16", vnet2.AddressSpace.AddressPrefixes[0], "Vnet2 Address space 10.1.0.0/16");
        }

        [Test]
        public void Test03_VnetWith2Subnets()
        {
            Assert.AreEqual(2, vnet1.Subnets.Count, "2 subnets");
            Assert.AreEqual(2, vnet2.Subnets.Count, "2 subnets");
        }

        [Test]
        public void Test04_Vnet1SubnetsCidr()
        {
            Subnet publicSubnet = GetVnet1PublicSubnet();
            var privateSubnet = vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/24");
            Assert.IsNotNull(publicSubnet);
            Assert.IsNotNull(privateSubnet);
        }


        [Test]
        public void Test05_Vnet2SubnetsCidr()
        {
            var publicSubnet = GetVnet2PublicSubnet();
            var privateSubnet = vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/24");
            Assert.IsNotNull(publicSubnet);
            Assert.IsNotNull(privateSubnet);
        }

        [Test]
        public void Test06_Vnet1PublicSubnetsRoutes()
        {
            var publicSubnet = GetVnet1PublicSubnet();
            publicSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet1.Name, publicSubnet.Name, "RouteTable");

            var localRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/16" && c.NextHopType == "VnetLocal");
            var internetRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "0.0.0.0/0" && c.NextHopType == "Internet");

            Assert.IsNotNull(localRoute);
            Assert.IsNotNull(internetRoute);
        }

        [Test]
        public void Test07_Vnet2PublicSubnetsRoutes()
        {
            Subnet publicSubnet = GetVnet2PublicSubnet();
            publicSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet2.Name, publicSubnet.Name, "RouteTable");

            var localRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/16" && c.NextHopType == "VnetLocal");
            var internetRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "0.0.0.0/0" && c.NextHopType == "Internet");

            Assert.IsNotNull(localRoute);
            Assert.IsNotNull(internetRoute);
        }


        [Test]
        public void Test08_Vnet1PrivateSubnetsRoutes()
        {
            Subnet privateSubnet = GetVnet1PrivateSubnet();
            privateSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet1.Name, privateSubnet.Name, "RouteTable");

            var localRoute = privateSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/16" && c.NextHopType == "VnetLocal");
            Assert.IsNotNull(localRoute);
        }



        [Test]
        public void Test09_Vnet2PrivateSubnetsRoutes()
        {
            Subnet privateSubnet = GetVnet2PrivateSubnet();
            privateSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet2.Name, privateSubnet.Name, "RouteTable");

            var localRoute = privateSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/16" && c.NextHopType == "VnetLocal");
            Assert.IsNotNull(localRoute);
        }



        [Test]
        public void Test10_Vnet1PrivateSubnetsNatGateway()
        {
            var privateSubnet = GetVnet1PrivateSubnet();
            privateSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet1.Name, privateSubnet.Name, "NatGateway");
            Assert.IsNotNull(privateSubnet.NatGateway);
            var natGatewayId = privateSubnet.NatGateway.Id;
            var natGateways = client.NatGateways.List(Constants.ResourceGroupName);
            var natGateway = natGateways.FirstOrDefault(c => c.Id == natGatewayId);

            Assert.AreEqual("Standard", natGateway.Sku.Name);
            Assert.AreEqual("1", natGateway.Zones[0]);
        }

        [Test]
        public void Test11_VnetGlobalPeering()
        {
            VirtualNetworkPeering virtualNetworkPeering = vnet1.VirtualNetworkPeerings[0];
            Assert.IsNotNull(virtualNetworkPeering);
            Assert.AreEqual(virtualNetworkPeering.RemoteVirtualNetwork.Id, vnet2.Id);
            Assert.IsTrue(virtualNetworkPeering.AllowForwardedTraffic);
            Assert.IsTrue(virtualNetworkPeering.AllowVirtualNetworkAccess);
            Assert.IsFalse(virtualNetworkPeering.AllowGatewayTransit);
        }

        [Test]
        public void Test12_Vnet1PublicSubnetNetworkSecurityGroup()
        {
            var publicSubnet = GetVnet1PublicSubnet();
            publicSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet1.Name, publicSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = publicSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowHttpInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "80");
            Assert.AreEqual("Allow", allowHttpInbound.Access);
            Assert.AreEqual("Inbound", allowHttpInbound.Direction);
            Assert.AreEqual("*", allowHttpInbound.SourcePortRange);
            Assert.AreEqual("*", allowHttpInbound.SourceAddressPrefix);
            Assert.AreEqual("Tcp", allowHttpInbound.Protocol);
            Assert.AreEqual(201, allowHttpInbound.Priority);
            Assert.AreEqual(publicSubnet.AddressPrefix, allowHttpInbound.DestinationAddressPrefixes[0]);

            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("Tcp", allowAllTcpOutbound.Protocol);
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);
        }

        [Test]
        public void Test13_Vnet2PublicSubnetNetworkSecurityGroup()
        {
            var publicSubnet = GetVnet2PublicSubnet();
            publicSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet2.Name, publicSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = publicSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowHttpInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "80");
            Assert.AreEqual("Allow", allowHttpInbound.Access);
            Assert.AreEqual("Inbound", allowHttpInbound.Direction);
            Assert.AreEqual("*", allowHttpInbound.SourcePortRange);
            Assert.AreEqual("*", allowHttpInbound.SourceAddressPrefix);
            Assert.AreEqual("Tcp", allowHttpInbound.Protocol);
            Assert.AreEqual(201, allowHttpInbound.Priority);
            Assert.AreEqual(publicSubnet.AddressPrefix, allowHttpInbound.DestinationAddressPrefixes[0]);

            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("Tcp", allowAllTcpOutbound.Protocol);
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);
        }

        [Test]
        public void Test14_Vnet1PrivateSubnetNetworkSecurityGroup()
        {
            var privateSubnet = GetVnet1PrivateSubnet();
            privateSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet1.Name, privateSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = privateSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("Tcp", allowAllTcpOutbound.Protocol);
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);

            var crossVnetInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.SourceAddressPrefix == GetVnet2PrivateSubnet().AddressPrefix);
            Assert.AreEqual("Allow", crossVnetInbound.Access);
            Assert.AreEqual("Inbound", crossVnetInbound.Direction);
            Assert.AreEqual("*", crossVnetInbound.SourcePortRange);
            Assert.AreEqual("Tcp", crossVnetInbound.Protocol);
            Assert.AreEqual("80", crossVnetInbound.DestinationPortRange);
            Assert.AreEqual(201, crossVnetInbound.Priority);
            Assert.AreEqual(privateSubnet.AddressPrefix, crossVnetInbound.DestinationAddressPrefixes[0]);
        }

        [Test]
        public void Test15_Vnet2PrivateSubnetNetworkSecurityGroup()
        {
            var privateSubnet = GetVnet2PrivateSubnet();
            privateSubnet = client.Subnets.Get(Constants.ResourceGroupName, vnet2.Name, privateSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = privateSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("Tcp", allowAllTcpOutbound.Protocol);
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);

            var crossVnetInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.SourceAddressPrefix == GetVnet1PrivateSubnet().AddressPrefix);
            Assert.AreEqual("Allow", crossVnetInbound.Access);
            Assert.AreEqual("Inbound", crossVnetInbound.Direction);
            Assert.AreEqual("*", crossVnetInbound.SourcePortRange);
            Assert.AreEqual("Tcp", crossVnetInbound.Protocol);
            Assert.AreEqual("80", crossVnetInbound.DestinationPortRange);
            Assert.AreEqual(201, crossVnetInbound.Priority);
            Assert.AreEqual(privateSubnet.AddressPrefix, crossVnetInbound.DestinationAddressPrefixes[0]);
        }
    }
}