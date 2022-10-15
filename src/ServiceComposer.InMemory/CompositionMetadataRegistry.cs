using System;
using System.Collections.Generic;

namespace ServiceComposer.InMemory
{
    class CompositionMetadataRegistry
    {
        internal HashSet<Type> Components { get; } = new();

        public void AddComponent(Type type)
        {
            Components.Add(type);
        }
    }
}