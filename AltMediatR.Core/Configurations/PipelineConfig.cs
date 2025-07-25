﻿using AltMediatR.Core.Behaviors;

namespace AltMediatR.Core.Configurations
{
    public class PipelineConfig
    {
        public List<Type> BehaviorsInOrder { get; } = new()
        {
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(RetryBehavior<,>),
            typeof(CachingBehavior<,>)
        };
    }
}
