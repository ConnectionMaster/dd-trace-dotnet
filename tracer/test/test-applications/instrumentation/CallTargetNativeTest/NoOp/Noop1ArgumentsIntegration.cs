using System;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;

namespace CallTargetNativeTest.NoOp
{
    /// <summary>
    /// NoOp Integration for 1 Arguments
    /// </summary>
    public static class Noop1ArgumentsIntegration
    {
        public const string SKIPMETHODBODY = "SKIPMETHODBODY";

        public static CallTargetState OnMethodBegin<TTarget, TArg1>(TTarget instance, TArg1 arg1)
            where TTarget : IInstance, IDuckType
        {
            Console.WriteLine($"ProfilerOK: BeginMethod(1)<{typeof(Noop1ArgumentsIntegration)}, {typeof(TTarget)}, {typeof(TArg1)}>({instance}, {arg1})");
            if (instance.Instance?.GetType().Name.Contains("ThrowOnBegin") == true)
            {
                Console.WriteLine("Exception thrown.");
                throw new Exception();
            }

            Console.WriteLine(arg1 as string);
            if (arg1 as string == SKIPMETHODBODY)
            {
                return new CallTargetState(null, instance.Instance, skipMethodBody: true);
            }

            return new CallTargetState(null, instance.Instance);
        }

        public static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
            where TTarget : IInstance, IDuckType
            where TReturn : IReturnValue, IDuckType
        {
            Console.WriteLine($"ProfilerOK: EndMethod(1)<{typeof(Noop1ArgumentsIntegration)}, {typeof(TTarget)}, {typeof(TReturn)}>({instance}, {returnValue}, {exception?.ToString() ?? "(null)"}, {state})");
            if (instance.Instance?.GetType().Name.Contains("ThrowOnEnd") == true)
            {
                Console.WriteLine("Exception thrown.");
                throw new Exception();
            }

            if (state.GetSkipMethodBody() && returnValue.Type == typeof(int))
            {
                // if we skip the method body, for an int return value we should have the default int value (0) as return value.
                // originally in the example we always return 42.
                if (returnValue.Instance is int and not 0)
                {
                    Console.WriteLine("Exception thrown.");
                    throw new Exception();
                }
            }

            return new CallTargetReturn<TReturn>(returnValue);;
        }

        public static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
            where TTarget : IInstance, IDuckType
            where TReturn : IReturnValue
        {
            Console.WriteLine($"ProfilerOK: EndMethodAsync(1)<{typeof(Noop1ArgumentsIntegration)}, {typeof(TTarget)}, {typeof(TReturn)}>({instance}, {returnValue}, {exception?.ToString() ?? "(null)"}, {state})");
            if (instance.Instance?.GetType().Name.Contains("ThrowOnAsyncEnd") == true)
            {
                Console.WriteLine("Exception thrown.");
                throw new Exception();
            }

            return returnValue;
        }

        public interface IInstance
        {
        }

        public interface IArg
        {
        }

        public interface IReturnValue
        {
        }
    }
}
