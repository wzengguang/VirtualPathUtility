namespace System.Web
{
    internal class HttpRuntimePathUtil
    {

        /// <summary>
        /// We know HttpRuntime.AppDomainAppVirtualPath is HttpContextProvider.Instance.GetContext().Request.PathBase.Value. 
        /// Notice: PathBase is not end with a trailing slash.
        /// </summary>
        internal static VirtualPath AppDomainAppVirtualPathObject
        {
            get
            {
                VirtualPath appDomainAppVPath = null; //VirtualPath.CreateNonRelativeTrailingSlash(HttpRuntime.AppDomainAppVirtualPath);
                return appDomainAppVPath;
            }
        }

        internal static String AppDomainAppVirtualPathString
        {
            get
            {
                return VirtualPath.GetVirtualPathString(AppDomainAppVirtualPathObject);
            }
        }

        public static String AppDomainAppVirtualPath
        {
            get
            {
                return VirtualPath.GetVirtualPathStringNoTrailingSlash(AppDomainAppVirtualPathObject);
            }
        }

    }
}
