﻿using System;

namespace BehaviourTree.Behaviours
{
    public sealed class ActionBehaviour<TContext> : BaseBehaviour<TContext>
    {
        private readonly Func<TContext, BehaviourStatus> _action;

        public ActionBehaviour(string name, Func<TContext, BehaviourStatus> action) : base(name)
        {
            _action = action;
        }

        protected override BehaviourStatus Update(TContext context)
        {
            return _action(context);
        }
    }
}
