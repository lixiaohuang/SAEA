﻿/****************************************************************************
*Copyright (c) 2018 yswenli All Rights Reserved.
*CLR版本： 2.1.4
*机器名称：WENLI-PC
*公司名称：wenli
*命名空间：SAEA.NatSocket
*文件名： Class1
*版本号： v5.0.0.1
*唯一标识：ef84e44b-6fa2-432e-90a2-003ebd059303
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/3/1 15:54:21
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/3/1 15:54:21
*修改人： yswenli
*版本号： v5.0.0.1
*描述：
*
*****************************************************************************/
using SAEA.NatSocket.Enums;
using SAEA.NatSocket.Utils;
using System;
using System.Net;

namespace SAEA.NatSocket.Base
{

    /// <summary>
    /// Represents a port forwarding entry in the NAT translation table.
    /// </summary>
    public class Mapping
    {
        private DateTime _expiration;
        private int _lifetime;
        internal MappingLifetime LifetimeType { get; set; }


        /// <summary>
        /// Gets the mapping's description. It is the value stored in the NewPortMappingDescription parameter. 
        /// The NewPortMappingDescription parameter is a human readable string that describes the connection. 
        /// It is used in sorme web interfaces of routers so the user can see which program is using what port.
        /// </summary>
        public string Description { get; internal set; }
        /// <summary>
        /// Gets the private ip.
        /// </summary>
        public IPAddress PrivateIP { get; internal set; }
        /// <summary>
        /// Gets the protocol.
        /// </summary>
        public Protocol Protocol { get; internal set; }
        /// <summary>
        /// The PrivatePort parameter specifies the port on a client machine to which all traffic 
        /// coming in on <see cref="#PublicPort">PublicPort</see> for the protocol specified by 
        /// <see cref="#Protocol">Protocol</see> should be forwarded to.
        /// </summary>
        /// <see cref="Protocol">Protocol enum</see>
        public int PrivatePort { get; internal set; }
        /// <summary>
        /// Gets the public ip.
        /// </summary>
        public IPAddress PublicIP { get; internal set; }
        /// <summary>
        /// Gets the external (visible) port number.
        /// It is the value stored in the NewExternalPort parameter .
        /// The NewExternalPort parameter is used to specify the TCP or UDP port on the WAN side of the router which should be forwarded. 
        /// </summary>
        public int PublicPort { get; internal set; }
        /// <summary>
        /// Gets the lifetime. The Lifetime parameter tells the router how long the portmapping should be active. 
        /// Since most programs don't know this in advance, it is often set to 0, which means 'unlimited' or 'permanent'.
        /// </summary>
        /// <remarks>
        /// All portmappings are release automatically as part of the shutdown process when <see cref="NatDiscoverer">NatUtility</see>.<see cref="NatUtility#releaseonshutdown">ReleaseOnShutdown</see> is true.
        /// Permanent portmappings will not be released if the process ends anormally.
        /// Since most programs don't know the lifetime in advance, SAEA.NatSocket renew all the portmappings (except the permanents) before they expires. So, developers have to close explicitly those portmappings
        /// they don't want to remain open for the session.
        /// </remarks>
        public int Lifetime
        {
            get { return _lifetime; }
            internal set
            {
                switch (value)
                {
                    case int.MaxValue:
                        LifetimeType = MappingLifetime.Session;
                        _lifetime = 10 * 60; // ten minutes
                        _expiration = DateTime.UtcNow.AddSeconds(_lifetime); ;
                        break;
                    case 0:
                        LifetimeType = MappingLifetime.Permanent;
                        _lifetime = 0;
                        _expiration = DateTime.UtcNow;
                        break;
                    default:
                        LifetimeType = MappingLifetime.Manual;
                        _lifetime = value;
                        _expiration = DateTime.UtcNow.AddSeconds(_lifetime);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the expiration. The property value is calculated using <see cref="#Lifetime">Lifetime</see> property.
        /// </summary>
        public DateTime Expiration
        {
            get { return _expiration; }
            internal set
            {
                _expiration = value;
                _lifetime = (int)(_expiration - DateTime.UtcNow).TotalSeconds;
            }
        }

        internal Mapping(Protocol protocol, IPAddress privateIP, int privatePort, int publicPort)
            : this(protocol, privateIP, privatePort, publicPort, 0, "Open.Nat")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mapping"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="privateIP">The private ip.</param>
        /// <param name="privatePort">The private port.</param>
        /// <param name="publicPort">The public port.</param>
        /// <param name="lifetime">The lifetime.</param>
        /// <param name="description">The description.</param>
        public Mapping(Protocol protocol, IPAddress privateIP, int privatePort, int publicPort, int lifetime, string description)
        {
            Guard.IsInRange(privatePort, 0, ushort.MaxValue, "privatePort");
            Guard.IsInRange(publicPort, 0, ushort.MaxValue, "publicPort");
            Guard.IsInRange(lifetime, 0, int.MaxValue, "lifetime");
            Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, "protocol");
            Guard.IsNotNull(privateIP, "privateIP");

            Protocol = protocol;
            PrivateIP = privateIP;
            PrivatePort = privatePort;
            PublicIP = IPAddress.None;
            PublicPort = publicPort;
            Lifetime = lifetime;
            Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mapping"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="privatePort">The private port.</param>
        /// <param name="publicPort">The public port.</param>
        /// <remarks>
        /// This constructor initializes a Permanent mapping. The description by deafult is "SAEA.NatSocket"
        /// </remarks>
        public Mapping(Protocol protocol, int privatePort, int publicPort)
            : this(protocol, IPAddress.None, privatePort, publicPort, 0, "SAEA.NatSocket")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mapping"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="privatePort">The private port.</param>
        /// <param name="publicPort">The public port.</param>
        /// <param name="description">The description.</param>
        /// <remarks>
        /// This constructor initializes a Permanent mapping.
        /// </remarks>
        public Mapping(Protocol protocol, int privatePort, int publicPort, string description)
            : this(protocol, IPAddress.None, privatePort, publicPort, 0, description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mapping"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="privatePort">The private port.</param>
        /// <param name="publicPort">The public port.</param>
        /// <param name="lifetime">The lifetime.</param>
        /// <param name="description">The description.</param>
        public Mapping(Protocol protocol, int privatePort, int publicPort, int lifetime, string description)
            : this(protocol, IPAddress.None, privatePort, publicPort, lifetime, description)
        {
        }

        internal Mapping(Mapping mapping)
        {
            PrivateIP = mapping.PrivateIP;
            PrivatePort = mapping.PrivatePort;
            Protocol = mapping.Protocol;
            PublicIP = mapping.PublicIP;
            PublicPort = mapping.PublicPort;
            LifetimeType = mapping.LifetimeType;
            Description = mapping.Description;
            _lifetime = mapping._lifetime;
            _expiration = mapping._expiration;
        }

        /// <summary>
        /// Determines whether this instance is expired.
        /// </summary>
        /// <remarks>
        /// Permanent mappings never expires.
        /// </remarks>
        public bool IsExpired()
        {
            return LifetimeType != MappingLifetime.Permanent
                && LifetimeType != MappingLifetime.ForcedSession
                && Expiration < DateTime.UtcNow;
        }

        internal bool ShoundRenew()
        {
            return LifetimeType == MappingLifetime.Session && IsExpired();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var m = obj as Mapping;
            if (ReferenceEquals(null, m)) return false;
            return PublicPort == m.PublicPort && PrivatePort == m.PrivatePort;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PublicPort;
                hashCode = (hashCode * 397) ^ (PrivateIP != null ? PrivateIP.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ PrivatePort;
                return hashCode;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} {1} --> {2}:{3} ({4})",
                                    Protocol == Protocol.Tcp ? "Tcp" : "Udp",
                                    PublicPort,
                                    PrivateIP,
                                    PrivatePort,
                                    Description);
        }
    }
}
