//==============================================================================
// Copyright (c) 2017-2018 Fiats Inc. All rights reserved.
// http://www.fiats.asia/
//

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;
using System.Reactive.Disposables;

namespace Fiats.Utils
{
    public static class EnumUtil
    {
        public static string ToEnumString<TEnum>(this TEnum type) where TEnum : struct
        {
            var enumType = typeof(TEnum);
            var name = Enum.GetName(enumType, type);
            var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
            return enumMemberAttribute.Value;
        }
    }

    public static class RxUtil
    {
        public static TResult AddTo<TResult>(this TResult resource, CompositeDisposable disposable) where TResult : IDisposable
        {
            disposable.Add(resource);
            return resource;
        }
    }

    public static class DebugEx
    {
        [Conditional("DEBUG")]
        public static void Trace()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)));
        }

        [Conditional("DEBUG")]
        public static void Trace(string format, params object[] args)
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + format, args);
        }

        [Conditional("DEBUG")]
        public static void EnterMethod()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + "EnterMethod");
        }

        [Conditional("DEBUG")]
        public static void ExitMethod()
        {
            Debug.WriteLine(CreatePrifix(new StackFrame(1, true)) + "ExitMethod");
        }

        static string CreatePrifix(StackFrame sf)
        {
            var method = sf.GetMethod();
            var methodname = method.DeclaringType + "." + method.Name;
            var fileName = Path.GetFileName(sf.GetFileName());
            var lineNum = sf.GetFileLineNumber();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("{0}({1}):{2}({3}) ", fileName, lineNum, methodname, threadId);
        }
    }
}
