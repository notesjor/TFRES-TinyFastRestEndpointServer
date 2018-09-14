using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatsonWebserver
{
    internal class StaticRouteManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private List<StaticRoute> Routes;
        private readonly object RouteLock;

        #endregion

        #region Constructors-and-Factories

        public StaticRouteManager()
        {
            Routes = new List<StaticRoute>();
            RouteLock = new object();
        }

        #endregion

        #region Public-Methods

        public void Add(string verb, string path, Func<HttpRequest, HttpResponse> handler)
        {
            if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var r = new StaticRoute(verb, path, handler);
            Add(r);
        }

        public StaticRoute Get(string verb, string path)
        {
            if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            verb = verb.ToLower();
            path = path.ToLower();
            if (!path.StartsWith("/")) path = "/" + path;
            if (!path.EndsWith("/")) path = path + "/";

            lock (RouteLock)
            {
                StaticRoute curr = Routes.FirstOrDefault(i => i.Verb == verb && i.Path == path);
                if (curr == null || curr == default(StaticRoute))
                {
                    return null;
                }
                else
                {
                    return curr;
                }
            }
        }

        public bool Exists(string verb, string path)
        {
            if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
             
            verb = verb.ToLower();
            path = path.ToLower();
            if (!path.StartsWith("/")) path = "/" + path;
            if (!path.EndsWith("/")) path = path + "/";

            lock (RouteLock)
            {
                StaticRoute curr = Routes.FirstOrDefault(i => i.Verb == verb && i.Path == path);
                if (curr == null || curr == default(StaticRoute))
                { 
                    return false;
                }
            }
             
            return true;
        }

        public Func<HttpRequest, HttpResponse> Match(string verb, string path)
        {
            if (string.IsNullOrEmpty(verb)) throw new ArgumentNullException(nameof(verb));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            verb = verb.ToLower();
            path = path.ToLower();
            if (!path.StartsWith("/")) path = "/" + path;
            if (!path.EndsWith("/")) path = path + "/";

            lock (RouteLock)
            {
              return Routes.FirstOrDefault(i => i.Verb == verb && i.Path == path)?.Handler;
            }
        }

        #endregion

        #region Private-Methods

        private void Add(StaticRoute route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));

            route.Verb = route.Verb.ToLower();
            route.Path = route.Path.ToLower();
            if (!route.Path.StartsWith("/")) route.Path = "/" + route.Path;
            if (!route.Path.EndsWith("/")) route.Path = route.Path + "/";

            if (Exists(route.Verb, route.Path))
            { 
                return;
            }

            lock (RouteLock)
            {
                Routes.Add(route); 
            }
        }

        #endregion
    }
}
