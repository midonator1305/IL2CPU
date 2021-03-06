using System;

namespace IL2CPU.API.Attribs
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class Plug : Attribute
    {
        public Plug()
        {
        }

        public Plug(Type target)
        {
            Target = target ?? throw new ArgumentNullException();
            return;
        }

        public Plug(string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) { throw new ArgumentNullException(); }
            TargetName = targetName;
            return;
        }

        public Type Target { get; set; }

        public string TargetName { get; set; }

        public bool IsOptional
        {
            get;
            set;
        }

        public bool Inheritable = false;
    }
}
