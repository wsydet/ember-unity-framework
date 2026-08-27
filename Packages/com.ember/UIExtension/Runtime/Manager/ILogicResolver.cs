//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Reflection;
//
//namespace Burner.UIExtension
//{
//    public interface ILogicResolver : IDisposable
//    {
//        GameUILogic ResolveAndCreateLogic(string className);
//    }
//
//    public class AssemblyLogicResolver : ILogicResolver
//    {
//        Dictionary<string, Type> logicHandlers = new Dictionary<string, Type>();
//
//        public AssemblyLogicResolver(Assembly assembly)
//        {
//            var types = assembly.GetTypes();
//            var baseType = typeof(GameUILogic);
//            foreach(var t in types)
//            {
//                if (t.IsGenericTypeDefinition || t.ContainsGenericParameters)
//                    continue;
//                if (t.IsAbstract)
//                    continue;
//                if (baseType.IsAssignableFrom(t))
//                {
//                    logicHandlers[t.Name] = t;
//                }
//            }
//        }
//
//        public void Dispose()
//        {
//            
//        }
//
//        public GameUILogic ResolveAndCreateLogic(string className)
//        {
//            if (logicHandlers.TryGetValue(className, out var type))
//            {
//                var obj = Activator.CreateInstance(type);
//                GameUILogic logic = obj as GameUILogic;
//                if (logic == null)
//                    Logger.Error($"Cannot create:{className}, type missmatch, got:{obj.GetType().BaseType}");
//                return logic;
//            }
//            return null;
//        }
//    }
//}
