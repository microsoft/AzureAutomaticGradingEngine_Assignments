using AzureProjectTest.Helper;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using NUnit.Framework;

namespace AzureProjectTest
{
    [GameClass(1)]
    [Parallelizable(ParallelScope.Children)]
    public class VnetTests
    {
        private sealed class TestScope : IDisposable
        {

            public readonly NetworkManagementClient client;
            public readonly VirtualNetwork vnet1;
            public readonly VirtualNetwork vnet2;
            public TestScope()
            {
                var config = new Config();
                client = new NetworkManagementClient(config.Credentials);
                client.SubscriptionId = config.SubscriptionId;
                vnet1 = client.VirtualNetworks.Get(Constants.ResourceGroupName, Constants.Vnet1Name);
                vnet2 = client.VirtualNetworks.Get(Constants.ResourceGroupName, Constants.Vnet2Name);
            }

            public Subnet GetVnet1PublicSubnet()
            {
                return vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.1.0/24");
            }


            public Subnet GetVnet2PublicSubnet()
            {
                return vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.1.0/24");
            }

            public Subnet GetVnet1PrivateSubnet()
            {
                return vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/24");
            }

            public Subnet GetVnet2PrivateSubnet()
            {
                return vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/24");
            }

            public void Dispose()
            {
                client.Dispose();
            }
        }



        [Test]
        public void Test01_Have2VnetsIn2Regions()
        {
            using var scope = new TestScope();
            Assert.IsNotNull(scope.vnet1);
            Assert.AreEqual("southeastasia", scope.vnet1.Location);
            Assert.IsNotNull(scope.vnet2);
            Assert.AreEqual("eastasia", scope.vnet2.Location);
        }

        [Test]
        public void Test02_VnetAddressSpace()
        {
            using var scope = new TestScope();
            Assert.AreEqual("10.0.0.0/16", scope.vnet1.AddressSpace.AddressPrefixes[0], "Vnet1 Address space 10.0.0.0/16");
            Assert.AreEqual("10.1.0.0/16", scope.vnet2.AddressSpace.AddressPrefixes[0], "Vnet2 Address space 10.1.0.0/16");
        }

        [Test]
        public void Test03_VnetWith2Subnets()
        {
            using var scope = new TestScope();
            Assert.AreEqual(2, scope.vnet1.Subnets.Count, "2 subnets");
            Assert.AreEqual(2, scope.vnet2.Subnets.Count, "2 subnets");
        }

        [Test]
        public void Test04_Vnet1SubnetsCidr()
        {
            using var scope = new TestScope();
            Subnet publicSubnet = scope.GetVnet1PublicSubnet();
            var privateSubnet = scope.vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/24");
            Assert.IsNotNull(publicSubnet);
            Assert.IsNotNull(privateSubnet);
        }


        [Test]
        public void Test05_Vnet2SubnetsCidr()
        {
            using var scope = new TestScope();
            var publicSubnet = scope.GetVnet2PublicSubnet();
            var privateSubnet = scope.vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/24");
            Assert.IsNotNull(publicSubnet);
            Assert.IsNotNull(privateSubnet);
        }

        [Test]
        public void Test06_Vnet1PublicSubnetsRoutes()
        {
            using var scope = new TestScope();
            var publicSubnet = scope.GetVnet1PublicSubnet();
            publicSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet1.Name, publicSubnet.Name, "RouteTable");

            var localRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/16" && c.NextHopType == "VnetLocal");
            var internetRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "0.0.0.0/0" && c.NextHopType == "Internet");

            Assert.IsNotNull(localRoute);
            Assert.IsNotNull(internetRoute);
        }

        [Test]
        public void Test07_Vnet2PublicSubnetsRoutes()
        {
            using var scope = new TestScope();
            Subnet publicSubnet = scope.GetVnet2PublicSubnet();
            publicSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet2.Name, publicSubnet.Name, "RouteTable");

            var localRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/16" && c.NextHopType == "VnetLocal");
            var internetRoute = publicSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "0.0.0.0/0" && c.NextHopType == "Internet");

            Assert.IsNotNull(localRoute);
            Assert.IsNotNull(internetRoute);
        }


        [Test]
        public void Test08_Vnet1PrivateSubnetsRoutes()
        {
            using var scope = new TestScope();
            Subnet privateSubnet = scope.GetVnet1PrivateSubnet();
            privateSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet1.Name, privateSubnet.Name, "RouteTable");

            var localRoute = privateSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/16" && c.NextHopType == "VnetLocal");
            Assert.IsNotNull(localRoute);
        }



        [Test]
        public void Test09_Vnet2PrivateSubnetsRoutes()
        {
            using var scope = new TestScope();
            Subnet privateSubnet = scope.GetVnet2PrivateSubnet();
            privateSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet2.Name, privateSubnet.Name, "RouteTable");

            var localRoute = privateSubnet.RouteTable.Routes.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/16" && c.NextHopType == "VnetLocal");
            Assert.IsNotNull(localRoute);
        }



        [Test]
        public void Test10_Vnet1PrivateSubnetsNatGateway()
        {
            using var scope = new TestScope();
            var privateSubnet = scope.GetVnet1PrivateSubnet();
            privateSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet1.Name, privateSubnet.Name, "NatGateway");
            Assert.IsNotNull(privateSubnet.NatGateway);
            var natGatewayId = privateSubnet.NatGateway.Id;
            var natGateways = scope.client.NatGateways.List(Constants.ResourceGroupName);
            var natGateway = natGateways.FirstOrDefault(c => c.Id == natGatewayId);

            Assert.AreEqual("Standard", natGateway.Sku.Name);
            Assert.AreEqual("1", natGateway.Zones[0]);
        }

        [Test]
        public void Test11_VnetGlobalPeering()
        {
            using var scope = new TestScope();
            VirtualNetworkPeering virtualNetworkPeering = scope.vnet1.VirtualNetworkPeerings[0];
            Assert.IsNotNull(virtualNetworkPeering);
            Assert.AreEqual(virtualNetworkPeering.RemoteVirtualNetwork.Id, scope.vnet2.Id);
            Assert.IsTrue(virtualNetworkPeering.AllowForwardedTraffic);
            Assert.IsTrue(virtualNetworkPeering.AllowVirtualNetworkAccess);
            Assert.IsFalse(virtualNetworkPeering.AllowGatewayTransit);
        }

        [Test]
        public void Test12_Vnet1PublicSubnetNetworkSecurityGroup()
        {
            using var scope = new TestScope();
            var publicSubnet = scope.GetVnet1PublicSubnet();
            publicSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet1.Name, publicSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = publicSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowHttpInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "80");
            Assert.AreEqual("Allow", allowHttpInbound.Access);
            Assert.AreEqual("Inbound", allowHttpInbound.Direction);
            Assert.AreEqual("*", allowHttpInbound.SourcePortRange);
            Assert.AreEqual("*", allowHttpInbound.SourceAddressPrefix);
            Assert.AreEqual("TCP", allowHttpInbound.Protocol.ToUpper());
            Assert.AreEqual(201, allowHttpInbound.Priority);
            Assert.IsTrue(publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefix || publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefixes[0]);


            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);
        }

        [Test]
        public void Test13_Vnet2PublicSubnetNetworkSecurityGroup()
        {
            using var scope = new TestScope();
            var publicSubnet = scope.GetVnet2PublicSubnet();
            publicSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet2.Name, publicSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = publicSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowHttpInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "80");
            Assert.AreEqual("Allow", allowHttpInbound.Access);
            Assert.AreEqual("Inbound", allowHttpInbound.Direction);
            Assert.AreEqual("*", allowHttpInbound.SourcePortRange);
            Assert.AreEqual("*", allowHttpInbound.SourceAddressPrefix);
            Assert.AreEqual("TCP", allowHttpInbound.Protocol.ToUpper());
            Assert.AreEqual(201, allowHttpInbound.Priority);
            Assert.IsTrue(publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefix || publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefixes[0]);

            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);
        }

        [Test]
        public void Test14_Vnet1PrivateSubnetNetworkSecurityGroup()
        {
            using var scope = new TestScope();
            var privateSubnet = scope.GetVnet1PrivateSubnet();
            privateSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet1.Name, privateSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = privateSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);

            var crossVnetInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.SourceAddressPrefix == scope.GetVnet2PrivateSubnet().AddressPrefix);
            Assert.AreEqual("Allow", crossVnetInbound.Access);
            Assert.AreEqual("Inbound", crossVnetInbound.Direction);
            Assert.AreEqual("*", crossVnetInbound.SourcePortRange);
            Assert.AreEqual("TCP", crossVnetInbound.Protocol.ToUpper());
            Assert.AreEqual("80", crossVnetInbound.DestinationPortRange);
            Assert.AreEqual(201, crossVnetInbound.Priority);
            Assert.IsTrue(privateSubnet.AddressPrefix == crossVnetInbound.DestinationAddressPrefix || privateSubnet.AddressPrefix == crossVnetInbound.DestinationAddressPrefixes[0]);
        }

        [Test]
        public void Test15_Vnet2PrivateSubnetNetworkSecurityGroup()
        {
            using var scope = new TestScope();
            var privateSubnet = scope.GetVnet2PrivateSubnet();
            privateSubnet = scope.client.Subnets.Get(Constants.ResourceGroupName, scope.vnet2.Name, privateSubnet.Name, "NetworkSecurityGroup");
            var networkSecurityGroup = privateSubnet.NetworkSecurityGroup;
            Assert.IsNotNull(networkSecurityGroup);

            var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
            Assert.AreEqual("Allow", allowAllTcpOutbound.Access);
            Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
            Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
            Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
            Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
            Assert.AreEqual(100, allowAllTcpOutbound.Priority);
            Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);

            var crossVnetInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.SourceAddressPrefix == scope.GetVnet1PrivateSubnet().AddressPrefix);
            Assert.AreEqual("Allow", crossVnetInbound.Access);
            Assert.AreEqual("Inbound", crossVnetInbound.Direction);
            Assert.AreEqual("*", crossVnetInbound.SourcePortRange);
            Assert.AreEqual("TCP", crossVnetInbound.Protocol.ToUpper());
            Assert.AreEqual("80", crossVnetInbound.DestinationPortRange);
            Assert.AreEqual(201, crossVnetInbound.Priority);
            Assert.IsTrue(privateSubnet.AddressPrefix == crossVnetInbound.DestinationAddressPrefix || privateSubnet.AddressPrefix == crossVnetInbound.DestinationAddressPrefixes[0]);
        }
    }
}