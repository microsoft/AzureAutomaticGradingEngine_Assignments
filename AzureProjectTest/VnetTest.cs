using AzureProjectTest.Helper;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using NUnit.Framework;

namespace AzureProjectTest;

[GameClass(3)]
[Parallelizable(ParallelScope.Children), Timeout(Constants.TIMEOUT)]
public class VnetTests
{
    [GameTask(
        "Can you create 2 vnets? First vnet named 'projVnet1Prod' in 'southeastasia' of CIDR '10.0.0.0/16' and " +
        "Second vnet named 'projVnet2Prod' in 'eastasia' of CIDR '10.1.0.0/16'.",
        2, 10, 1)]
    [Test]
    public void Test01_Have2VnetsIn2Regions()
    {
        using var scope = new TestScope();
        Assert.IsNotNull(scope.Vnet1);
        Assert.AreEqual("southeastasia", scope.Vnet1.Location);
        Assert.IsNotNull(scope.Vnet2);
        Assert.AreEqual("eastasia", scope.Vnet2.Location);
    }

    [GameTask(1)]
    [Test]
    public void Test02_VnetAddressSpace()
    {
        using var scope = new TestScope();
        Assert.AreEqual("10.0.0.0/16", scope.Vnet1.AddressSpace.AddressPrefixes[0], "Vnet1 Address space 10.0.0.0/16");
        Assert.AreEqual("10.1.0.0/16", scope.Vnet2.AddressSpace.AddressPrefixes[0], "Vnet2 Address space 10.1.0.0/16");
    }

    [GameTask(
    "Can you create 2 subnets in vnet named 'projVnet1Prod? CIDR 10.0.1.0/24 and 10.0.0.0/24. Then" +
        " create 2 subnets in vnet named 'projVnet2Prod' CIDR 10.1.1.0/24 and 10.1.0.0/24",
    5, 20, 2)]
    [Test]
    public void Test03_VnetWith2Subnets()
    {
        using var scope = new TestScope();
        Assert.AreEqual(2, scope.Vnet1.Subnets.Count, "2 subnets");
        Assert.AreEqual(2, scope.Vnet2.Subnets.Count, "2 subnets");
    }

    [GameTask(2)]
    [Test]
    public void Test04_Vnet1SubnetsCidr()
    {
        using var scope = new TestScope();
        var publicSubnet = scope.GetVnet1PublicSubnet();
        var privateSubnet = scope.GetVnet1PrivateSubnet();
        Assert.IsNotNull(publicSubnet);
        Assert.IsNotNull(privateSubnet);
    }

    [GameTask(2)]
    [Test]
    public void Test05_Vnet2SubnetsCidr()
    {
        using var scope = new TestScope();
        var publicSubnet = scope.GetVnet2PublicSubnet();
        var privateSubnet = scope.GetVnet2PrivateSubnet();
        Assert.IsNotNull(publicSubnet);
        Assert.IsNotNull(privateSubnet);
    }

    [GameTask("Can you set 2 routes in vnet named 'projVnet1Prod VnetLocal and Internet for 10.0.1.0/24 subnet? To make it as a public subnet.", 5, 10)]
    [Test]
    public void Test06_Vnet1PublicSubnetsRoutes()
    {
        using var scope = new TestScope();
        var publicSubnet = scope.GetVnet1PublicSubnet();
        publicSubnet = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet1.Name, publicSubnet.Name,
            "RouteTable");

        var localRoute =
            publicSubnet.RouteTable.Routes.FirstOrDefault(c =>
                c.AddressPrefix == "10.0.0.0/16" && c.NextHopType == "VnetLocal");
        var internetRoute =
            publicSubnet.RouteTable.Routes.FirstOrDefault(c =>
                c.AddressPrefix == "0.0.0.0/0" && c.NextHopType == "Internet");

        Assert.IsNotNull(localRoute);
        Assert.IsNotNull(internetRoute);
    }

    [GameTask("Can you set 2 routes in vnet named 'projVnet2Prod VnetLocal and Internet for 10.1.1.0/24 subnet? To make it as a public subnet.", 5, 10)]
    [Test]
    public void Test07_Vnet2PublicSubnetsRoutes()
    {
        using var scope = new TestScope();
        var publicSubnet = scope.GetVnet2PublicSubnet();
        publicSubnet = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet2.Name, publicSubnet.Name,
            "RouteTable");

        var localRoute =
            publicSubnet.RouteTable.Routes.FirstOrDefault(c =>
                c.AddressPrefix == "10.1.0.0/16" && c.NextHopType == "VnetLocal");
        var internetRoute =
            publicSubnet.RouteTable.Routes.FirstOrDefault(c =>
                c.AddressPrefix == "0.0.0.0/0" && c.NextHopType == "Internet");

        Assert.IsNotNull(localRoute);
        Assert.IsNotNull(internetRoute);
    }

    [GameTask("Can you set 2 routes in vnet named 'projVnet1Prod VnetLocal for 10.0.0.0/24 subnet? To prepare it as a private subnet.", 5, 10)]
    [Test]
    public void Test08_Vnet1PrivateSubnetsRoutes()
    {
        using var scope = new TestScope();
        var privateSubnet = scope.GetVnet1PrivateSubnet();
        privateSubnet = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet1.Name, privateSubnet.Name,
            "RouteTable");

        var localRoute =
            privateSubnet.RouteTable.Routes.FirstOrDefault(c =>
                c.AddressPrefix == "10.0.0.0/16" && c.NextHopType == "VnetLocal");
        Assert.IsNotNull(localRoute);
    }

    [GameTask("Can you set 2 routes in vnet named 'projVnet2Prod VnetLocal for 10.1.0.0/24 subnet? To prepare it as a private subnet.", 5, 10)]

    [Test]
    public void Test09_Vnet2PrivateSubnetsRoutes()
    {
        using var scope = new TestScope();
        var privateSubnet = scope.GetVnet2PrivateSubnet();
        privateSubnet = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet2.Name, privateSubnet.Name,
            "RouteTable");

        var localRoute =
            privateSubnet.RouteTable.Routes.FirstOrDefault(c =>
                c.AddressPrefix == "10.1.0.0/16" && c.NextHopType == "VnetLocal");
        Assert.IsNotNull(localRoute);
    }

    [GameTask("Can you add a Standard NAT Gateway at zone 1 for subnet 10.0.1.0/24 ? ", 5, 10)]

    [Test]
    public void Test10_Vnet1PublicSubnetsNatGateway()
    {
        using var scope = new TestScope();
        var publicSubnet = scope.GetVnet1PublicSubnet();
        publicSubnet = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet1.Name, publicSubnet.Name,
            "NatGateway");
        Assert.IsNotNull(publicSubnet.NatGateway);
        var natGatewayId = publicSubnet.NatGateway.Id;
        var natGateways = scope.Client.NatGateways.List(Constants.ResourceGroupName);
        var natGateway = natGateways.FirstOrDefault(c => c.Id == natGatewayId);

        Assert.AreEqual("Standard", natGateway!.Sku.Name);
        Assert.AreEqual("1", natGateway.Zones[0]);
    }

    [GameTask("Can you add a Virtual Network Peering from projVnet1Prod to projVnet2Prod (Remote)? " +
        "Allow Forwarded Traffic and Virtual Network Access but not allow Gateway Transit.", 5, 10)]

    [Test]
    public void Test11_VnetGlobalPeering()
    {
        using var scope = new TestScope();
        var virtualNetworkPeering = scope.Vnet1.VirtualNetworkPeerings[0];
        Assert.IsNotNull(virtualNetworkPeering);
        Assert.AreEqual(virtualNetworkPeering.RemoteVirtualNetwork.Id, scope.Vnet2.Id);
        Assert.IsTrue(virtualNetworkPeering.AllowForwardedTraffic);
        Assert.IsTrue(virtualNetworkPeering.AllowVirtualNetworkAccess);
        Assert.IsFalse(virtualNetworkPeering.AllowGatewayTransit);
    }

    [GameTask("Can you add 2 Network Security Rules to subnet 10.0.1.0/24? " +
        "First rule allows connect to HTTP from anywhere with priority 201." +
        "Second rule allows all TCP outbound to anywhere with priority 100.", 5, 10)]
    [Test]
    public void Test12_Vnet1PublicSubnetNetworkSecurityGroup()
    {
        using var scope = new TestScope();
        var publicSubnet = scope.GetVnet1PublicSubnet();
        publicSubnet = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet1.Name, publicSubnet.Name,
            "NetworkSecurityGroup");
        var networkSecurityGroup = publicSubnet.NetworkSecurityGroup;
        Assert.IsNotNull(networkSecurityGroup);

        var allowHttpInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "80");
        Assert.AreEqual("Allow", allowHttpInbound!.Access);
        Assert.AreEqual("Inbound", allowHttpInbound.Direction);
        Assert.AreEqual("*", allowHttpInbound.SourcePortRange);
        Assert.AreEqual("*", allowHttpInbound.SourceAddressPrefix);
        Assert.AreEqual("TCP", allowHttpInbound.Protocol.ToUpper());
        Assert.AreEqual(201, allowHttpInbound.Priority);
        Assert.IsTrue(publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefix ||
                      publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefixes[0]);


        var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
        Assert.AreEqual("Allow", allowAllTcpOutbound!.Access);
        Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
        Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
        Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
        Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
        Assert.AreEqual(100, allowAllTcpOutbound.Priority);
        Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);
    }

    [GameTask("Can you add 2 Network Security Rules to subnet 10.1.1.0/24? " +
    "First rule allows connect to HTTP from anywhere with priority 201." +
    "Second rule allows all TCP outbound to anywhere with priority 100.", 5, 10)]
    [Test]
    public void Test13_Vnet2PublicSubnetNetworkSecurityGroup()
    {
        using var scope = new TestScope();
        var publicSubnet = scope.GetVnet2PublicSubnet();
        publicSubnet = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet2.Name, publicSubnet.Name,
            "NetworkSecurityGroup");
        var networkSecurityGroup = publicSubnet.NetworkSecurityGroup;
        Assert.IsNotNull(networkSecurityGroup);

        var allowHttpInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "80");
        Assert.AreEqual("Allow", allowHttpInbound!.Access);
        Assert.AreEqual("Inbound", allowHttpInbound.Direction);
        Assert.AreEqual("*", allowHttpInbound.SourcePortRange);
        Assert.AreEqual("*", allowHttpInbound.SourceAddressPrefix);
        Assert.AreEqual("TCP", allowHttpInbound.Protocol.ToUpper());
        Assert.AreEqual(201, allowHttpInbound.Priority);
        Assert.IsTrue(publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefix ||
                      publicSubnet.AddressPrefix == allowHttpInbound.DestinationAddressPrefixes[0]);

        var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
        Assert.AreEqual("Allow", allowAllTcpOutbound!.Access);
        Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
        Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
        Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
        Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
        Assert.AreEqual(100, allowAllTcpOutbound.Priority);
        Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);
    }

    [GameTask("Can you add 2 Network Security Rules to subnet 10.1.1.0/24? " +
"First rule allows HTTP cross vent in bound from 10.1.0.0/24 with priority 201." +
"Second rule allows all TCP outbound to anywhere with priority 100.", 5, 10)]
    [Test]
    public void Test14_Vnet1PrivateSubnetNetworkSecurityGroup()
    {
        using var scope = new TestScope();
        var privateSubnet1 = scope.GetVnet1PrivateSubnet();
        privateSubnet1 = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet1.Name, privateSubnet1.Name,
            "NetworkSecurityGroup");
        var networkSecurityGroup = privateSubnet1.NetworkSecurityGroup;
        Assert.IsNotNull(networkSecurityGroup);

        var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
        Assert.AreEqual("Allow", allowAllTcpOutbound!.Access);
        Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
        Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
        Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
        Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
        Assert.AreEqual(100, allowAllTcpOutbound.Priority);
        Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);

        var crossVnetInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c =>
            c.SourceAddressPrefix == scope.GetVnet2PrivateSubnet().AddressPrefix);
        Assert.AreEqual("Allow", crossVnetInbound!.Access);
        Assert.AreEqual("Inbound", crossVnetInbound.Direction);
        Assert.AreEqual("*", crossVnetInbound.SourcePortRange);
        Assert.AreEqual("TCP", crossVnetInbound.Protocol.ToUpper());
        Assert.AreEqual("80", crossVnetInbound.DestinationPortRange);
        Assert.AreEqual(201, crossVnetInbound.Priority);
        Assert.IsTrue(privateSubnet1.AddressPrefix == crossVnetInbound.DestinationAddressPrefix ||
                      privateSubnet1.AddressPrefix == crossVnetInbound.DestinationAddressPrefixes[0]);
    }

    [GameTask("Can you add 2 Network Security Rules to subnet 10.1.0.0/24? " +
"First rule allows HTTP cross vent in bound from 10.0.0.0/24 with priority 201." +
"Second rule allows all TCP outbound to anywhere with priority 100.", 5, 10)]
    [Test]
    public void Test15_Vnet2PrivateSubnetNetworkSecurityGroup()
    {
        using var scope = new TestScope();
        var privateSubnet2 = scope.GetVnet2PrivateSubnet();
        privateSubnet2 = scope.Client.Subnets.Get(Constants.ResourceGroupName, scope.Vnet2.Name, privateSubnet2.Name,
            "NetworkSecurityGroup");
        var networkSecurityGroup = privateSubnet2.NetworkSecurityGroup;
        Assert.IsNotNull(networkSecurityGroup);

        var allowAllTcpOutbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c => c.DestinationPortRange == "*");
        Assert.AreEqual("Allow", allowAllTcpOutbound!.Access);
        Assert.AreEqual("Outbound", allowAllTcpOutbound.Direction);
        Assert.AreEqual("*", allowAllTcpOutbound.SourcePortRange);
        Assert.AreEqual("*", allowAllTcpOutbound.SourceAddressPrefix);
        Assert.AreEqual("TCP", allowAllTcpOutbound.Protocol.ToUpper());
        Assert.AreEqual(100, allowAllTcpOutbound.Priority);
        Assert.AreEqual("*", allowAllTcpOutbound.DestinationAddressPrefix);

        var crossVnetInbound = networkSecurityGroup.SecurityRules.FirstOrDefault(c =>
            c.SourceAddressPrefix == scope.GetVnet1PrivateSubnet().AddressPrefix);
        Assert.AreEqual("Allow", crossVnetInbound!.Access);
        Assert.AreEqual("Inbound", crossVnetInbound.Direction);
        Assert.AreEqual("*", crossVnetInbound.SourcePortRange);
        Assert.AreEqual("TCP", crossVnetInbound.Protocol.ToUpper());
        Assert.AreEqual("80", crossVnetInbound.DestinationPortRange);
        Assert.AreEqual(201, crossVnetInbound.Priority);
        Assert.IsTrue(privateSubnet2.AddressPrefix == crossVnetInbound.DestinationAddressPrefix ||
                      privateSubnet2.AddressPrefix == crossVnetInbound.DestinationAddressPrefixes[0]);
    }

    private sealed class TestScope : IDisposable
    {
        public readonly NetworkManagementClient Client;
        public readonly VirtualNetwork Vnet1;
        public readonly VirtualNetwork Vnet2;

        public TestScope()
        {
            var config = new Config();
            Client = new NetworkManagementClient(config.Credentials);
            Client.SubscriptionId = config.SubscriptionId;
            Vnet1 = Client.VirtualNetworks.Get(Constants.ResourceGroupName, Constants.Vnet1Name);
            Vnet2 = Client.VirtualNetworks.Get(Constants.ResourceGroupName, Constants.Vnet2Name);
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public Subnet GetVnet1PublicSubnet()
        {
            return Vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.1.0/24");
        }


        public Subnet GetVnet2PublicSubnet()
        {
            return Vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.1.0/24");
        }

        public Subnet GetVnet1PrivateSubnet()
        {
            return Vnet1.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.0.0.0/24");
        }

        public Subnet GetVnet2PrivateSubnet()
        {
            return Vnet2.Subnets.FirstOrDefault(c => c.AddressPrefix == "10.1.0.0/24");
        }
    }
}
