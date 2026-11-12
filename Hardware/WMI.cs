using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using WmiLight;

namespace ZenStates.Core.Hardware
{
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
                // Unknown properties raise COMException, not WmiException.
                Debug.WriteLine(ex.Message);
            }

            return retval;
        }

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

        public static WmiInstance Query(string scope, string wmiClass)
        {
            return QueryInternal(scope, string.Format("SELECT * FROM {0}", wmiClass));
        }

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
                    // WmiLight releases IWbemServices on connection dispose; ownership moves to the instance.
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
                            string name = wmiClass.ClassName;

                            if (string.IsNullOrEmpty(name))
                            {
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
                }

                return name;
            }
        }

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

                // The embedded object shares its parent's native resources; the parent must stay alive.
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
