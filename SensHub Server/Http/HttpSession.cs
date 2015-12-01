using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Server.Http
{
    public class HttpSession
    {
        /// <summary>
        /// Session ID
        /// </summary>
        public Guid UUID { get; private set; }

        /// <summary>
        /// Session variables.
        /// </summary>
        public Dictionary<string, object> Variables { get; private set; }

        /// <summary>
        /// If this session has been authenticated
        /// </summary>
        public bool Authenticated { get; set; }

        /// <summary>
        /// The client address associated with the session
        /// </summary>
        public string RemoteAddress { get; set; }

        /// <summary>
        /// When the session was last accessed
        /// </summary>
        public DateTime LastAccess { get; set; }

        internal HttpSession()
        {
            UUID = Guid.NewGuid();
            Variables = new Dictionary<string, object>();
        }
    }
}
