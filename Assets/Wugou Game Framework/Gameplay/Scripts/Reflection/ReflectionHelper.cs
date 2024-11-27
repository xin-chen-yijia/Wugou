using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Wugou
{
    /// <summary>
    /// 反射使用过程中的一些优化及功能总结
    /// https://www.cnblogs.com/xinaixia/p/5777886.html
    /// </summary>
    public static class ReflectionHelper
    {
        public interface ISetValue
        {
            void Set(object target, object val);
        }

        public class SetterWrapper<TTarget, TValue> : ISetValue
        {
            private Action<TTarget, TValue> _setter;

            public SetterWrapper(PropertyInfo propertyInfo)
            {
                if (propertyInfo == null)
                    throw new ArgumentNullException("propertyInfo");

                if (propertyInfo.CanWrite == false)
                    throw new NotSupportedException("属性不支持写操作。");

                MethodInfo m = propertyInfo.GetSetMethod(true);
                _setter = (Action<TTarget, TValue>)Delegate.CreateDelegate(typeof(Action<TTarget, TValue>), null, m);
            }

            //public SetterWrapper(FieldInfo fieldInfo)
            //{
            //    if (fieldInfo == null)
            //        throw new ArgumentNullException("propertyInfo");


            //    MethodInfo m = fieldInfo.GetSetMethod(true);
            //    _setter = (Action<TTarget, TValue>)Delegate.CreateDelegate(typeof(Action<TTarget, TValue>), null, m);
            //}

            public void SetValue(TTarget target, TValue val)
            {
                _setter(target, val);
            }

            void ISetValue.Set(object target, object val)
            {
                _setter((TTarget)target, (TValue)val);
            }

        }
        public static ISetValue CreatePropertySetterWrapper(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
            if (propertyInfo.CanWrite == false)
                throw new NotSupportedException("属性不支持写操作。");

            MethodInfo mi = propertyInfo.GetSetMethod(true);

            if (mi.GetParameters().Length > 1)
                throw new NotSupportedException("不支持构造索引器属性的委托。");

            Type instanceType = typeof(SetterWrapper<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            return (ISetValue)Activator.CreateInstance(instanceType, propertyInfo);
        }

        // TODO: SetterWrapper无FieldInfo参数的构造函数
        //public static ISetValue CreateFieldSetterWrapper(FieldInfo fieldInfo)
        //{
        //    if (fieldInfo == null)
        //        throw new ArgumentNullException("fieldInfo");

        //    Type instanceType = typeof(SetterWrapper<,>).MakeGenericType(fieldInfo.DeclaringType, fieldInfo.FieldType);
        //    return (ISetValue)Activator.CreateInstance(instanceType, fieldInfo);
        //}

        /// <summary>
        /// 设置字段的值, 委托方法
        /// </summary>
        /// <typeparam name="TObj"></typeparam>
        /// <typeparam name="TField"></typeparam>
        /// <param name="fieldinfo"></param>
        /// <returns></returns>
        public static Action<TObj, TValue> CreateFieldSetterDelegate<TObj, TValue>(FieldInfo fieldinfo)
        {
            var method = new DynamicMethod("SetField", typeof(void), new Type[] { typeof(TObj), typeof(TValue) }, fieldinfo.DeclaringType, true);
            ILGenerator il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fieldinfo);
            il.Emit(OpCodes.Ret);

            return (Action<TObj, TValue>)method.CreateDelegate(typeof(Action<TObj, TValue>));
        }


        public interface IGetValue
        {
            object Get(object target);
        }

        public class GetterWrapper<TTarget, TValue> : IGetValue
        {
            private Func<TTarget, TValue> _getter;

            public GetterWrapper(PropertyInfo propertyInfo)
            {
                if (propertyInfo == null)
                    throw new ArgumentNullException("propertyInfo");

                if (propertyInfo.CanRead == false)
                    throw new InvalidOperationException("属性不支持读操作。");

                MethodInfo m = propertyInfo.GetGetMethod(true);
                _getter = (Func<TTarget, TValue>)Delegate.CreateDelegate(typeof(Func<TTarget, TValue>), null, m);
            }

            public TValue GetValue(TTarget target)
            {
                return _getter(target);
            }
            object IGetValue.Get(object target)
            {
                return _getter((TTarget)target);
            }
        }

        public static IGetValue CreatePropertyGetterWrapper(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
            if (propertyInfo.CanRead == false)
                throw new InvalidOperationException("属性不支持读操作。");

            MethodInfo mi = propertyInfo.GetGetMethod(true);

            if (mi.GetParameters().Length > 0)
                throw new NotSupportedException("不支持构造索引器属性的委托。");

            Type instanceType = typeof(GetterWrapper<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            return (IGetValue)Activator.CreateInstance(instanceType, propertyInfo);
        }

        public static IGetValue CreateFieldGetterWrapper(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException("propertyInfo");

            Type instanceType = typeof(GetterWrapper<,>).MakeGenericType(fieldInfo.DeclaringType, fieldInfo.FieldType);
            return (IGetValue)Activator.CreateInstance(instanceType, fieldInfo);
        }
    }
}
