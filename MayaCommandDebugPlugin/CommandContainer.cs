using Autodesk.Maya.OpenMaya;
using System;
using System.Linq;
using System.Reflection;

namespace MayaCommandDebugPlugin
{
    public class PluginContainer : MarshalByRefObject
    {
        private Assembly _assembly;

        private Type _pluginType;
        private IExtensionPlugin _pluginObj;

        private Type _commandType;
        private MPxCommand _commandObj;

        private bool _isUndoable;


        public override object InitializeLifetimeService() { return null; }


        public void LoadDll(string dllPath)
        {
            DebugHelper.DumpMethod();
            _assembly = Assembly.LoadFrom(dllPath);
        }


        public void InitializePlugin()
        {
            DebugHelper.DumpMethod();
            if (_pluginObj != null) return;

            if (_pluginType == null)
            {
                foreach (var typeInfo in _assembly.DefinedTypes)
                {
                    var type = typeInfo.AsType();
                    if (typeof(IExtensionPlugin).IsAssignableFrom(type))
                    {
                        _pluginType = type;
                        break;
                    }
                }
            }

            _pluginObj = (IExtensionPlugin)Activator.CreateInstance(_pluginType);
            _pluginObj.InitializePlugin();
        }


        public void UninitializePlugin()
        {
            DebugHelper.DumpMethod();
            if (_pluginObj == null) return;

            _pluginObj.UninitializePlugin();
            _pluginObj = null;
        }


        public bool isUndoable()
        {
            DebugHelper.DumpMethod();
            return _isUndoable;
        }


        public object doIt(string className, byte[] args)
        {
            DebugHelper.DumpMethod();

            if (_commandType == null)
            {
                _commandType = _assembly.DefinedTypes
                    .Where(type => typeof(MPxCommand).IsAssignableFrom(type))
                    .First();
            }

            _commandObj = (MPxCommand)Activator.CreateInstance(_commandType);
            var argl = MArgListSerializer.Deserialize(args);

            try
            {
                _commandObj.doIt(argl);
                _isUndoable = _commandObj.isUndoable();
            }
            catch (Exception e)
            {
                MGlobal.displayError(e.Message);
            }

            return null;
        }


        public void redoIt()
        {
            if (_commandObj is IUndoMPxCommand)
            {
                try
                {
                    _commandObj.redoIt();
                }
                catch (Exception e)
                {
                    MGlobal.displayError(e.Message);
                }
            }
        }


        public void undoIt()
        {
            try
            {
                _commandObj.undoIt();
            }
            catch (Exception e)
            {
                MGlobal.displayError(e.Message);
            }
        }

    }
}
