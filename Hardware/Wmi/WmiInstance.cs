using System;
using WmiLight;

namespace ZenStates.Core.Hardware
{
    /// <summary>
    /// AOT-safe stand-in for <c>System.Management.ManagementObject</c> / <c>ManagementBaseObject</c>,
    /// backed by <see cref="WmiObject"/> from WmiLight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifetime: a <see cref="WmiInstance"/> handed out by <see cref="WMI.Query"/> or
    /// <see cref="WMI.GetInstance"/> <b>owns</b> the underlying <see cref="WmiConnection"/> and disposes it
    /// together with itself. Instances wrapping an embedded (child) object - e.g. the <c>pack</c> sub-object
    /// of a WMI method's out parameters - do <b>not</b> own the connection: WmiLight releases the native
    /// <c>IWbemServices</c> pointer on connection dispose, so the connection must outlive every object
    /// obtained from it. Callers must therefore keep the parent instance alive while reading a child.
    /// </para>
    /// <para>
    /// Ownership of native <see cref="WmiObject"/>s obtained from an enumerator is transferred to the
    /// returned instance: WmiLight allocates a fresh wrapper per <c>MoveNext()</c> and the enumerator's
    /// Dispose only releases the enumerator itself.
    /// </para>
    /// </remarks>
    public sealed class WmiInstance : IDisposable
    {
        private readonly WmiConnection connection;
        private WmiObject wmiObject;
        private bool disposed;

        /// <summary>
        /// Wraps an object that was obtained from <paramref name="connection"/>; the instance takes
        /// ownership of the connection and disposes it.
        /// </summary>
        internal WmiInstance(WmiConnection connection, WmiObject wmiObject)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            this.connection = connection;
            this.wmiObject = wmiObject ?? throw new ArgumentNullException(nameof(wmiObject));
        }

        /// <summary>
        /// Wraps a child object (embedded instance) that is owned by another, still living instance.
        /// </summary>
        internal WmiInstance(WmiObject wmiObject)
            : this(wmiObject, null)
        {
        }

        private WmiInstance(WmiObject wmiObject, WmiConnection connection)
        {
            this.connection = connection;
            this.wmiObject = wmiObject ?? throw new ArgumentNullException(nameof(wmiObject));
        }

        /// <summary>
        /// The underlying WmiLight object. Accessing it after disposal throws.
        /// </summary>
        internal WmiObject Native
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(WmiInstance));

                return wmiObject;
            }
        }

        /// <summary>
        /// The full WMI object path, e.g. <c>\\MACHINE\root\wmi:AMD_ACPI.InstanceName="ACPI"</c>.
        /// </summary>
        public string Path
        {
            get { return Native.Path; }
        }

        /// <summary>
        /// The WMI class name of this object.
        /// </summary>
        public string ClassName
        {
            get { return Native.Class; }
        }

        /// <summary>
        /// Gets or sets a property value by name. Equivalent to <c>ManagementBaseObject[name]</c>.
        /// </summary>
        public object this[string propertyName]
        {
            get { return GetPropertyValue(propertyName); }
            set { SetPropertyValue(propertyName, value); }
        }

        /// <summary>
        /// Property accessor shaped like <c>ManagementBaseObject.Properties[name].Value</c>.
        /// </summary>
        public PropertyCollection Properties
        {
            get { return new PropertyCollection(this); }
        }

        /// <summary>
        /// Reads a property. Embedded instances come back as <see cref="WmiInstance"/> (or
        /// <see cref="WmiInstance"/>[] for embedded arrays), matching the way
        /// <c>ManagementBaseObject.Properties[name].Value</c> exposed nested objects.
        /// </summary>
        public object GetPropertyValue(string propertyName)
        {
            object value = Native.GetPropertyValue(propertyName);

            WmiObject single = value as WmiObject;
            if (single != null)
                return new WmiInstance(single);

            WmiObject[] many = value as WmiObject[];
            if (many != null)
            {
                WmiInstance[] instances = new WmiInstance[many.Length];
                for (int i = 0; i < many.Length; i++)
                    instances[i] = new WmiInstance(many[i]);

                return instances;
            }

            return value;
        }

        /// <summary>
        /// Strongly typed variant of <see cref="GetPropertyValue(string)"/>.
        /// </summary>
        public T GetPropertyValue<T>(string propertyName)
        {
            object value = GetPropertyValue(propertyName);
            return value == null ? default(T) : (T)value;
        }

        /// <summary>
        /// Sets a property value by name. Used for method in-parameters.
        /// </summary>
        /// <remarks>
        /// WmiLight only exposes typed setters, so the value is dispatched by type. Byte arrays cannot be
        /// marshalled by WmiLight 7.x (<c>WmiObject</c> has no <c>byte[]</c> setter) - the only consumer of
        /// that shape was <c>WMI.RunCommand</c>, which is not part of this build.
        /// </remarks>
        public void SetPropertyValue(string propertyName, object value)
        {
            WmiObject target = Native;

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (value is uint) { target.SetPropertyValue(propertyName, (uint)value); return; }
            if (value is int) { target.SetPropertyValue(propertyName, (int)value); return; }
            if (value is byte) { target.SetPropertyValue(propertyName, (byte)value); return; }
            if (value is sbyte) { target.SetPropertyValue(propertyName, (sbyte)value); return; }
            if (value is short) { target.SetPropertyValue(propertyName, (short)value); return; }
            if (value is ushort) { target.SetPropertyValue(propertyName, (ushort)value); return; }
            if (value is long) { target.SetPropertyValue(propertyName, (long)value); return; }
            if (value is ulong) { target.SetPropertyValue(propertyName, (ulong)value); return; }
            if (value is bool) { target.SetPropertyValue(propertyName, (bool)value); return; }
            if (value is string) { target.SetPropertyValue(propertyName, (string)value); return; }
            if (value is string[]) { target.SetPropertyValue(propertyName, (string[])value); return; }

            if (value is byte[])
                throw new NotSupportedException(
                    "WmiLight cannot marshal byte[] method parameters; the byte[] based WMI command path is unavailable.");

            TypeCode typeCode = Type.GetTypeCode(value.GetType());
            switch (typeCode)
            {
                case TypeCode.UInt32: target.SetPropertyValue(propertyName, Convert.ToUInt32(value)); return;
                case TypeCode.Int32: target.SetPropertyValue(propertyName, Convert.ToInt32(value)); return;
                case TypeCode.Byte: target.SetPropertyValue(propertyName, Convert.ToByte(value)); return;
                case TypeCode.Int16: target.SetPropertyValue(propertyName, Convert.ToInt16(value)); return;
                case TypeCode.UInt16: target.SetPropertyValue(propertyName, Convert.ToUInt16(value)); return;
                case TypeCode.Int64: target.SetPropertyValue(propertyName, Convert.ToInt64(value)); return;
                case TypeCode.UInt64: target.SetPropertyValue(propertyName, Convert.ToUInt64(value)); return;
                case TypeCode.Boolean: target.SetPropertyValue(propertyName, Convert.ToBoolean(value)); return;
                case TypeCode.String: target.SetPropertyValue(propertyName, Convert.ToString(value)); return;
            }

            throw new NotSupportedException(
                "Unsupported WMI property type: " + value.GetType().FullName);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            WmiObject obj = wmiObject;
            wmiObject = null;
            if (obj != null)
                obj.Dispose();

            // Only the instance that created the connection releases it; child objects leave it alone.
            if (connection != null)
                connection.Dispose();
        }

        /// <summary>
        /// Minimal <c>ManagementBaseObject.Properties</c> replacement.
        /// </summary>
        public sealed class PropertyCollection
        {
            private readonly WmiInstance owner;

            internal PropertyCollection(WmiInstance owner)
            {
                this.owner = owner;
            }

            public WmiProperty this[string propertyName]
            {
                get { return new WmiProperty(owner, propertyName); }
            }
        }

        /// <summary>
        /// Single property accessor exposing <see cref="Value"/>.
        /// </summary>
        public sealed class WmiProperty
        {
            private readonly WmiInstance owner;
            private readonly string name;

            internal WmiProperty(WmiInstance owner, string name)
            {
                this.owner = owner;
                this.name = name;
            }

            public object Value
            {
                get { return owner.GetPropertyValue(name); }
                set { owner.SetPropertyValue(name, value); }
            }
        }
    }
}
