using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using WmiLight;

namespace ZenStates.Core.Hardware
{
    /// <summary>
    /// WMI access layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class used to be built on <c>System.Management</c>, whose late-bound COM interop cannot be
    /// compiled by Native AOT. It is now implemented on top of WmiLight, which talks to WMI through a thin
    /// native shim and is therefore AOT-safe. The public member names, parameter names and semantics are
    /// unchanged; only the WMI object types were replaced by the AOT-safe <see cref="WmiScope"/> /
    /// <see cref="WmiInstance"/> wrappers.
    /// </para>
    /// <para>
    /// Removed: <c>RunCommand(WmiInstance, uint, uint)</c>. It marshalled a <c>byte[]</c> ("Inbuf") into a
    /// WMI method call, which WmiLight 7.x cannot express - <c>WmiObject</c> only exposes typed scalar and
    /// <c>string[]</c> setters and has no <c>byte[]</c> overload. It had no callers inside this assembly.
    /// </para>
    /// </remarks>
    public static class WMI
    {
        public static object TryGetProperty(WmiInstance wmiObj, string propertyName)
        {
            object retval = null;
            try
            {
                retval = wmiObj.GetPropertyValue(propertyName);
            }
            catch (Exception ex)
            {
                // An unknown property surfaces as a COMException (WBEM_E_NOT_FOUND) rather than a
                // WmiException; both must degrade to null, as ManagementObject.GetPropertyValue did.
                Debug.WriteLine(ex.Message);
            }

            return retval;
        }

        //root\wmi
        public static WmiScope Connect(string scope)
        {
            try
            {
                using (var sc = new ServiceController("Winmgmt"))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                        throw new InvalidOperationException(
                            "Windows Management Instrumentation service is not running");
                }

                WmiScope wmiScope = new WmiScope(scope);
                wmiScope.Connect();

                if (wmiScope.IsConnected)
                    return wmiScope;

                wmiScope.Dispose();
                throw new InvalidOperationException(string.Format("Failed to connect to {0}", scope));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("WMI: {0}", ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Returns the first instance of <paramref name="wmiClass"/>, detached from the query enumerator.
        /// The returned instance owns its WMI connection; dispose it when done.
        /// </summary>
        public static WmiInstance Query(string scope, string wmiClass)
        {
            return QueryInternal(scope, string.Format("SELECT * FROM {0}", wmiClass));
        }

        /// <summary>
        /// Returns the single instance of <paramref name="wmiClass"/> whose <c>InstanceName</c> matches.
        /// Equivalent to the previous <c>new ManagementObject(scope, "Class.InstanceName='x'", null)</c>.
        /// </summary>
        public static WmiInstance GetInstance(string scope, string wmiClass, string instanceName)
        {
            if (string.IsNullOrEmpty(instanceName))
                return null;

            // WQL string literal: a single quote is escaped by doubling it.
            string escaped = instanceName.Replace("'", "''");

            return QueryInternal(scope, string.Format(
                "SELECT * FROM {0} WHERE InstanceName='{1}'", wmiClass, escaped));
        }

        private static WmiInstance QueryInternal(string scope, string wql)
        {
            WmiConnection connection = null;
            bool handedOff = false;

            try
            {
                connection = new WmiConnection(scope);
                connection.Open();

                foreach (WmiObject obj in connection.CreateQuery(wql))
                {
                    // Ownership of the connection moves to the instance: WmiLight releases the native
                    // IWbemServices pointer on connection dispose, so the object must not outlive it.
                    handedOff = true;
                    return new WmiInstance(connection, obj);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            if (!handedOff && connection != null)
                connection.Dispose();

            return null;
        }

        public static List<string> GetWmiNamespaces(string root)
        {
            List<string> namespaces = new List<string>();
            List<string> children = new List<string>();

            try
            {
                using (WmiScope scope = new WmiScope(root))
                {
                    scope.Connect();

                    foreach (WmiObject obj in scope.Connection.CreateQuery("SELECT * FROM __namespace"))
                    {
                        using (var ns = new WmiInstance(obj))
                        {
                            object name = ns.GetPropertyValue("Name");
                            if (name == null)
                                continue;

                            string namespaceName = string.Concat(root, "\\", name);
                            namespaces.Add(namespaceName);
                            children.Add(namespaceName);
                        }
                    }
                }

                // Recurse only after the parent connection is released, so deep trees do not hold
                // one open connection per level.
                foreach (string child in children)
                    namespaces.AddRange(GetWmiNamespaces(child));

                namespaces.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return namespaces;
        }

        public static List<string> GetClassNamesWithinWmiNamespace(string wmiNamespaceName)
        {
            List<string> classNames = new List<string>();

            try
            {
                using (WmiScope scope = new WmiScope(wmiNamespaceName))
                {
                    scope.Connect();

                    foreach (WmiObject obj in scope.Connection.CreateQuery("SELECT * FROM meta_class"))
                    {
                        using (var wmiClass = new WmiInstance(obj))
                        {
                            // For a class definition object __CLASS holds the class name itself.
                            string name = wmiClass.ClassName;

                            if (string.IsNullOrEmpty(name))
                            {
                                // Fallback: \\MACHINE\root\namespace:ClassName
                                string path = wmiClass.Path;
                                int separator = path == null ? -1 : path.LastIndexOf(':');
                                if (separator >= 0)
                                    name = path.Substring(separator + 1);
                            }

                            if (!string.IsNullOrEmpty(name))
                                classNames.Add(name);
                        }
                    }
                }

                classNames.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return classNames;
        }

        public static string GetInstanceName(string scope, string wmiClass)
        {
            using (WmiInstance queryObject = Query(scope, wmiClass))
            {
                string name = "";

                if (queryObject == null)
                    return name;

                try
                {
                    var obj = TryGetProperty(queryObject, "InstanceName");
                    if (obj != null) name = obj.ToString();
                }
                catch
                {
                    // ignored
                }

                return name;
            }
        }

        /// <summary>
        /// Invokes <paramref name="methodName"/> on <paramref name="mo"/> and returns the out-parameter
        /// object, or <c>null</c> when the method does not exist or the call fails.
        /// </summary>
        public static WmiInstance InvokeMethod(WmiInstance mo, string methodName, string inParamName, uint arg)
        {
            if (mo == null)
                return null;

            WmiMethod method = null;
            WmiMethodParameters inParams = null;

            try
            {
                method = mo.Native.GetMethod(methodName);
                if (method == null)
                    return null;

                if (method.HasInParameters)
                {
                    inParams = method.CreateInParameters();

                    if (inParams != null && !string.IsNullOrEmpty(inParamName))
                        inParams.SetPropertyValue(inParamName, arg);
                }

                WmiMethodParameters outParams = null;

                if (inParams != null)
                    mo.Native.ExecuteMethod(method, inParams, out outParams);
                else
                    mo.Native.ExecuteMethod(method, out outParams);

                return outParams == null ? null : new WmiInstance(outParams);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                if (inParams != null)
                    inParams.Dispose();

                if (method != null)
                    method.Dispose();
            }
        }

        public static WmiInstance InvokeMethodAndGetValue(WmiInstance mo, string methodName, string propName,
            string inParamName, uint arg)
        {
            try
            {
                WmiInstance outParams = InvokeMethod(mo, methodName, inParamName, arg);
                if (outParams == null)
                    return null;

                // The embedded object shares the out-parameter object's native resources, so the parent is
                // deliberately not disposed here - the caller owns the returned instance and therefore its
                // backing storage.
                return (WmiInstance)outParams.GetPropertyValue(propName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
