using System;
using WmiLight;

namespace ZenStates.Core.Hardware
{
    // WmiLight releases IWbemServices on connection dispose; child instances must not outlive their parent.
    public sealed class WmiInstance : IDisposable
    {
        private readonly WmiConnection connection;
        private WmiObject wmiObject;
        private bool disposed;

        internal WmiInstance(WmiConnection connection, WmiObject wmiObject)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            this.connection = connection;
            this.wmiObject = wmiObject ?? throw new ArgumentNullException(nameof(wmiObject));
        }

        internal WmiInstance(WmiObject wmiObject)
            : this(wmiObject, null)
        {
        }

        private WmiInstance(WmiObject wmiObject, WmiConnection connection)
        {
            this.connection = connection;
            this.wmiObject = wmiObject ?? throw new ArgumentNullException(nameof(wmiObject));
        }

        internal WmiObject Native
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(WmiInstance));

                return wmiObject;
            }
        }

        public string Path
        {
            get { return Native.Path; }
        }

        public string ClassName
        {
            get { return Native.Class; }
        }

        public object this[string propertyName]
        {
            get { return GetPropertyValue(propertyName); }
            set { SetPropertyValue(propertyName, value); }
        }

        public PropertyCollection Properties
        {
            get { return new PropertyCollection(this); }
        }

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

        public T GetPropertyValue<T>(string propertyName)
        {
            object value = GetPropertyValue(propertyName);
            return value == null ? default(T) : (T)value;
        }

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

            if (connection != null)
                connection.Dispose();
        }

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
