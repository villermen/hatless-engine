﻿using System;

namespace HatlessEngine
{
    public class LogicalObject
    {
        private bool _Destroyed = false;
        public bool Destroyed
        { 
            get { return _Destroyed; }
            private set { _Destroyed = value; }
        }

        public LogicalObject()
        {
            //add object to Resource's objectlist
            Resources.AddObjects.Add(this);
        }

        public virtual void Step() { }
        internal virtual void AfterStep() { }

        public virtual void Draw() { }
        internal virtual void AfterDraw() { }

		public void Destroy()
        {
			OnDestroy();
            //add for removal from Resources.Objects (cant be done now because of iteration)
            Resources.RemoveObjects.Add(this);
            Destroyed = true;
        }
        public virtual void OnDestroy() { }
    }
}
