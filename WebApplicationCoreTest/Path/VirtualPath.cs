//------------------------------------------------------------------------------
// <copyright file="VirtualPath.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace System.Web
{
    using System.Web.Util;

    [Serializable]
    internal sealed class VirtualPath : IComparable
    {
        private string _appRelativeVirtualPath;
        private string _virtualPath;

        // const masks into the BitVector32
        private const int appRelativeAttempted = 0x00000004;

#pragma warning disable 0649
        private SimpleBitVector32 flags;
#pragma warning restore 0649


        internal static VirtualPath RootVirtualPath = VirtualPath.Create("/");

        private VirtualPath() { }

        // This is called to set the appropriate virtual path field when we already know
        // that the path is generally well formed.
        private VirtualPath(string virtualPath)
        {
            if (UrlPath.IsAppRelativePath(virtualPath))
            {
                _appRelativeVirtualPath = virtualPath;
            }
            else
            {
                _virtualPath = virtualPath;
            }
        }

        int IComparable.CompareTo(object obj)
        {

            VirtualPath virtualPath = obj as VirtualPath;

            // Make sure we're compared to another VirtualPath
            if (virtualPath == null)
                throw new ArgumentException();

            // Check if it's the same object
            if (virtualPath == this)
                return 0;

            return StringComparer.InvariantCultureIgnoreCase.Compare(
                this.VirtualPathString, virtualPath.VirtualPathString);
        }

        public string VirtualPathString
        {
            get
            {
                if (_virtualPath == null)
                {
                    //Debug.Assert(_appRelativeVirtualPath != null);

                    // This is not valid if we don't know the app path
                    if (HttpRuntimePathUtil.AppDomainAppVirtualPathObject == null)
                    {
                        throw new Exception($"cant make app absolute {_appRelativeVirtualPath}");
                    }

                    if (_appRelativeVirtualPath.Length == 1)
                    {
                        _virtualPath = HttpRuntimePathUtil.AppDomainAppVirtualPath;
                    }
                    else
                    {
                        _virtualPath = HttpRuntimePathUtil.AppDomainAppVirtualPathString +
                            _appRelativeVirtualPath.Substring(2);
                    }
                }

                return _virtualPath;
            }
        }

        internal string VirtualPathStringNoTrailingSlash
        {
            get
            {
                return UrlPath.RemoveSlashFromPathIfNeeded(VirtualPathString);
            }
        }

        // Return the virtual path string if we have it, otherwise null
        internal string VirtualPathStringIfAvailable
        {
            get
            {
                return _virtualPath;
            }
        }

        internal string AppRelativeVirtualPathStringOrNull
        {
            get
            {
                if (_appRelativeVirtualPath == null)
                {
                    //Debug.Assert(_virtualPath != null);

                    // If we already tried to get it and couldn't, return null
                    if (flags[appRelativeAttempted])
                        return null;

                    // This is not valid if we don't know the app path
                    if (HttpRuntimePathUtil.AppDomainAppVirtualPathObject == null)
                    {
                        throw new Exception($"VirtualPath {_virtualPath} cant make app relative");
                    }

                    _appRelativeVirtualPath = UrlPath.MakeVirtualPathAppRelativeOrNull(_virtualPath);

                    // Remember that we've attempted it
                    flags[appRelativeAttempted] = true;

                    // It could be null if it's not under the app root
                    if (_appRelativeVirtualPath == null)
                        return null;
                }

                return _appRelativeVirtualPath;
            }
        }

        // Return the app relative path if possible. Otherwise, settle for the absolute.
        public string AppRelativeVirtualPathString
        {
            get
            {
                string appRelativeVirtualPath = AppRelativeVirtualPathStringOrNull;
                return (appRelativeVirtualPath != null) ? appRelativeVirtualPath : _virtualPath;
            }
        }

        // Return the app relative virtual path string if we have it, otherwise null
        internal string AppRelativeVirtualPathStringIfAvailable
        {
            get
            {
                return _appRelativeVirtualPath;
            }
        }
        // Return the virtual string that's either app relative or not, depending on which
        // one we already have internally.  If we have both, we return absolute
        internal string VirtualPathStringWhicheverAvailable
        {
            get
            {
                return _virtualPath != null ? _virtualPath : _appRelativeVirtualPath;
            }
        }

        public string Extension
        {
            get
            {
                return UrlPath.GetExtension(VirtualPathString);
            }
        }

        public string FileName
        {
            get
            {
                return UrlPath.GetFileName(VirtualPathStringNoTrailingSlash);
            }
        }

        // If it's relative, combine it with the app root
        public VirtualPath CombineWithAppRoot()
        {
            return HttpRuntimePathUtil.AppDomainAppVirtualPathObject.Combine(this);
        }

        public VirtualPath Combine(VirtualPath relativePath)
        {
            if (relativePath == null)
                throw new ArgumentNullException("relativePath");

            // If it's not relative, return it unchanged
            if (!relativePath.IsRelative)
                return relativePath;

            // The base of the combine should never be relative
            FailIfRelativePath();

            // Get either _appRelativeVirtualPath or _virtualPath
            string virtualPath = VirtualPathStringWhicheverAvailable;

            // Combine it with the relative
            virtualPath = UrlPath.Combine(virtualPath, relativePath.VirtualPathString);

            // Set the appropriate virtual path in the new object
            return new VirtualPath(virtualPath);
        }



        ///////////// end of VirtualPathProvider methods /////////////




        internal void FailIfRelativePath()
        {
            if (this.IsRelative)
            {
                throw new ArgumentException($"VirtualPath {_virtualPath} allow relatvePath.");
            }
        }

        public bool IsRelative
        {
            get
            {
                // Note that we don't need to check for "~/", since _virtualPath never contains
                // app relative paths (_appRelativeVirtualPath does)
                return _virtualPath != null && _virtualPath[0] != '/';
            }
        }

        public bool IsRoot
        {
            get
            {
                return _virtualPath == "/";
            }
        }

        public VirtualPath Parent
        {
            get
            {
                // Getting the parent doesn't make much sense on relative paths
                FailIfRelativePath();

                // "/" doesn't have a parent, so return null
                if (IsRoot)
                    return null;

                // Get either _appRelativeVirtualPath or _virtualPath
                string virtualPath = VirtualPathStringWhicheverAvailable;

                // Get rid of the ending slash, otherwise we end up with Parent("/app/sub/") == "/app/sub/"
                virtualPath = UrlPath.RemoveSlashFromPathIfNeeded(virtualPath);

                // But if it's just "~", use the absolute path instead to get the parent
                if (virtualPath == "~")
                    virtualPath = VirtualPathStringNoTrailingSlash;

                int index = virtualPath.LastIndexOf('/');
                //Debug.Assert(index >= 0);

                // e.g. the parent of "/blah" is "/"
                if (index == 0)
                    return RootVirtualPath;

                // 

                // Get the parent
                virtualPath = virtualPath.Substring(0, index + 1);

                // Set the appropriate virtual path in the new object
                return new VirtualPath(virtualPath);
            }
        }

        internal static VirtualPath Combine(VirtualPath v1, VirtualPath v2)
        {

            // If the first is null, use the app root instead
            if (v1 == null)
            {
                v1 = HttpRuntimePathUtil.AppDomainAppVirtualPathObject;
            }

            // If the first is still null, return the second, unless it's relative
            if (v1 == null)
            {
                v2.FailIfRelativePath();
                return v2;
            }

            return v1.Combine(v2);
        }

        public static bool operator ==(VirtualPath v1, VirtualPath v2)
        {
            return VirtualPath.Equals(v1, v2);
        }

        public static bool operator !=(VirtualPath v1, VirtualPath v2)
        {
            return !VirtualPath.Equals(v1, v2);
        }

        public static bool Equals(VirtualPath v1, VirtualPath v2)
        {

            // Check if it's the same object
            if ((Object)v1 == (Object)v2)
            {
                return true;
            }

            if ((Object)v1 == null || (Object)v2 == null)
            {
                return false;
            }

            return EqualsHelper(v1, v2);
        }

        public override bool Equals(object value)
        {

            if (value == null)
                return false;

            VirtualPath virtualPath = value as VirtualPath;
            if ((object)virtualPath == null)
            {
                //Debug.Assert(false);
                return false;
            }

            return EqualsHelper(virtualPath, this);
        }

        private static bool EqualsHelper(VirtualPath v1, VirtualPath v2)
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(
                v1.VirtualPathString, v2.VirtualPathString) == 0;
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(VirtualPathString);
        }

        public override String ToString()
        {
            // If we only have the app relative path, and we don't know the app root, return
            // the app relative path instead of accessing VirtualPathString, which would throw
            if (_virtualPath == null && HttpRuntimePathUtil.AppDomainAppVirtualPathObject == null)
            {
                //Debug.Assert(_appRelativeVirtualPath != null);
                return _appRelativeVirtualPath;
            }

            return VirtualPathString;
        }



        internal static string GetVirtualPathString(VirtualPath virtualPath)
        {
            return virtualPath == null ? null : virtualPath.VirtualPathString;
        }

        internal static string GetVirtualPathStringNoTrailingSlash(VirtualPath virtualPath)
        {
            return virtualPath == null ? null : virtualPath.VirtualPathStringNoTrailingSlash;
        }

        // Default Create method
        public static VirtualPath Create(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAllPath);
        }

        public static VirtualPath CreateTrailingSlash(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAllPath | VirtualPathOptions.EnsureTrailingSlash);
        }

        public static VirtualPath CreateAllowNull(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAllPath | VirtualPathOptions.AllowNull);
        }

        public static VirtualPath CreateAbsolute(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAbsolutePath);
        }

        public static VirtualPath CreateNonRelative(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAbsolutePath | VirtualPathOptions.AllowAppRelativePath);
        }

        public static VirtualPath CreateAbsoluteTrailingSlash(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAbsolutePath | VirtualPathOptions.EnsureTrailingSlash);
        }

        public static VirtualPath CreateNonRelativeTrailingSlash(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAbsolutePath | VirtualPathOptions.AllowAppRelativePath |
                VirtualPathOptions.EnsureTrailingSlash);
        }

        public static VirtualPath CreateAbsoluteAllowNull(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAbsolutePath | VirtualPathOptions.AllowNull);
        }

        public static VirtualPath CreateNonRelativeAllowNull(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAbsolutePath | VirtualPathOptions.AllowAppRelativePath | VirtualPathOptions.AllowNull);
        }

        public static VirtualPath CreateNonRelativeTrailingSlashAllowNull(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAbsolutePath | VirtualPathOptions.AllowAppRelativePath |
                VirtualPathOptions.AllowNull | VirtualPathOptions.EnsureTrailingSlash);
        }

        public static VirtualPath Create(string virtualPath, VirtualPathOptions options)
        {

            // Trim it first, so that blank strings (e.g. "  ") get treated as empty
            if (virtualPath != null)
                virtualPath = virtualPath.Trim();

            // If it's empty, check whether we allow it
            if (String.IsNullOrEmpty(virtualPath))
            {
                if ((options & VirtualPathOptions.AllowNull) != 0)
                    return null;

                throw new ArgumentNullException("virtualPath");
            }

            // Dev10 767308: optimize for normal paths, and scan once for
            //     i) invalid chars
            //    ii) slashes
            //   iii) '.'

            bool slashes = false;
            bool dot = false;
            int len = virtualPath.Length;
            unsafe
            {
                fixed (char* p = virtualPath)
                {
                    for (int i = 0; i < len; i++)
                    {
                        switch (p[i])
                        {
                            // need to fix slashes ?
                            case '/':
                                if (i > 0 && p[i - 1] == '/')
                                    slashes = true;
                                break;
                            case '\\':
                                slashes = true;
                                break;
                            // contains "." or ".."
                            case '.':
                                dot = true;
                                break;
                            // invalid chars
                            case '\0':
                                throw new Exception($"Invalid path {virtualPath}");
                            default:
                                break;
                        }
                    }
                }
            }

            if (slashes)
            {
                // If we're supposed to fail on malformed path, then throw
                if ((options & VirtualPathOptions.FailIfMalformed) != 0)
                {
                    throw new Exception($"Invalid path {virtualPath}");
                }
                // Flip ----lashes, and remove duplicate slashes                
                virtualPath = UrlPath.FixVirtualPathSlashes(virtualPath);
            }

            // Make sure it ends with a trailing slash if requested
            if ((options & VirtualPathOptions.EnsureTrailingSlash) != 0)
                virtualPath = UrlPath.AppendSlashToPathIfNeeded(virtualPath);

            VirtualPath virtualPathObject = new VirtualPath();

            if (UrlPath.IsAppRelativePath(virtualPath))
            {

                if (dot)
                    virtualPath = UrlPath.ReduceVirtualPath(virtualPath);

                if (virtualPath[0] == UrlPath.appRelativeCharacter)
                {
                    if ((options & VirtualPathOptions.AllowAppRelativePath) == 0)
                    {
                        throw new ArgumentException($"virtualPath {virtualPath} allow appRelativePath");
                    }

                    virtualPathObject._appRelativeVirtualPath = virtualPath;
                }
                else
                {
                    // It's possible for the path to become absolute after calling Reduce,
                    // even though it started with "~/".  e.g. if the app is "/app" and the path is
                    // "~/../hello.aspx", it becomes "/hello.aspx", which is absolute

                    if ((options & VirtualPathOptions.AllowAbsolutePath) == 0)
                    {
                        throw new ArgumentException($"virtualPath {virtualPath} allow AbsolutePath");
                    }

                    virtualPathObject._virtualPath = virtualPath;
                }
            }
            else
            {
                if (virtualPath[0] != '/')
                {
                    if ((options & VirtualPathOptions.AllowRelativePath) == 0)
                    {
                        throw new ArgumentException($"virtualPath {virtualPath} allow relativePath");
                    }

                    // Don't Reduce relative paths, since the Reduce method is broken (e.g. "../foo.aspx" --> "/foo.aspx!")
                    // 
                    virtualPathObject._virtualPath = virtualPath;
                }
                else
                {
                    if ((options & VirtualPathOptions.AllowAbsolutePath) == 0)
                    {
                        throw new ArgumentException($"virtualPath {virtualPath} allow AbsolutePath");
                    }

                    if (dot)
                        virtualPath = UrlPath.ReduceVirtualPath(virtualPath);

                    virtualPathObject._virtualPath = virtualPath;
                }
            }
#if DBG
            virtualPathObject.ValidateState();
#endif
            return virtualPathObject;
        }
    }

    [Flags]
    internal enum VirtualPathOptions
    {
        AllowNull = 0x00000001,
        EnsureTrailingSlash = 0x00000002,
        AllowAbsolutePath = 0x00000004,
        AllowAppRelativePath = 0x00000008,
        AllowRelativePath = 0x00000010,
        FailIfMalformed = 0x00000020,

        AllowAllPath = AllowAbsolutePath | AllowAppRelativePath | AllowRelativePath,
    }
}
