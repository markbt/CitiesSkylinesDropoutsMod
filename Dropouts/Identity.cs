using System;

using ICities;

namespace Dropouts
{
    public class Identity : IUserMod
    {
        public string Name
        {
            get { return "Dropouts"; }
        }

        public string Description
        {
            get { return "Allows cims to flunk school."; }
        }
    }
}

