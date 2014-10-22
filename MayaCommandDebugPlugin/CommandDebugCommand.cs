using Autodesk.Maya.OpenMaya;
using System;
using System.IO;
using System.Reflection;

// This line is mandatory to declare a new command in Maya
// You need to change the last parameter without your own
// node name and unique ID
//#warning You need to change the Command name before continuing, then remove this line.
[assembly: MPxCommandClass(typeof(MayaCommandDebugPlugin.CommandDebugCommand), "commandDebug")]

namespace MayaCommandDebugPlugin
{
    // This class is instantiated by Maya each time when a command 
    // is called by the user or a script.
    [MPxCommandSyntaxFlag("-ld", "-load", Arg1=typeof(string))]
    [MPxCommandSyntaxFlag("-do", "-doIt", Arg1 = typeof(string))]
    [MPxCommandSyntaxFlag("-uld", "-unload")]
    public class CommandDebugCommand : MPxCommand, IMPxCommand, IUndoMPxCommand
    {
        // -ld でロードされて、-uld でアンロードされる
        private static AppDomain _domain;
        private static PluginContainer _container;
        private static string _pluginName;


        public CommandDebugCommand()
        {
            DebugHelper.DumpMethod();
        }


        override public void doIt(MArgList argl)
        {
            DebugHelper.DumpMethod();

            var argData = new MArgDatabase(syntax, argl);
            if (argData.isFlagSet("-ld"))
            {
                var nll = argData.flagArgumentString("-ld", 0);
                var result = LoadPluginImpl(nll);
                setResult(result);
            }
            else if (argData.isFlagSet("-do"))
            {
                var command = argData.flagArgumentString("-do", 0);
                // 呼び出し先のコマンドプラグイン内で setResult() されてるはずだから
                // ここでは何もしない
                DoItImpl(command, argl);
            }
            else if (argData.isFlagSet("-uld"))
            {
                var result = UnloadPluginImpl();
                setResult(result);
            }
            else
            {
                MGlobal.displayError("must assign flag '-ld/load' or '-uld/unload' or '-cmd/command_name'");
            }
        }


        public override bool isUndoable()
        {
            //
            // isUndoable() は本来 this のコンストラクト直後に呼ばれるため、
            // -load 指定時にも呼ばれてしまうことになる。
            // だが、このクラスは -doIt 時に _container をコンストラクトする仕様のため、
            // このときに _container.isUndoable() を呼ぶことができない。
            //
            // 仕方なく、this.isUndoable() ではひとまず true を返すことにして、
            // 実際にアンドゥが行われたとき（this.undoIt() が呼ばれたとき）に
            // _container.isUndoable() を呼び出すことにする。
            //
            // _container.isUndoable() が false を返せば実際にアンドゥは行われないとはいえ、
            // Maya の持つコマンドスタックに不正なアイテムが積まれてしまうことにはなる。
            //
            // しかし、実際にアンドゥが行われなければ動作にクリティカルな影響は与えないと考え、
            // それよりは、_container が内包するコマンドクラス（デバッグを行いたいコマンド）が
            // 意図通りのタイミング（-doIt 指定時）にコンストラクトされることを優先する。
            //
            return true;
        }


        public override void redoIt()
        {
            if (_container == null)
            {
                MGlobal.displayError("cannot call redoIt() before load");
                return;
            }
            _container.redoIt();
        }


        public override void undoIt()
        {
            if (_container == null)
            {
                MGlobal.displayError("cannot call undoIt() before load");
                return;
            }

            if (_container.isUndoable())
            {
                _container.undoIt();
            }
        }


        private string LoadPluginImpl(string nll)
        {
            if (_domain != null)
            {
                MGlobal.displayInfo(nll + " is already loaded");
                return "";
            }

            // .net framwork に正しくアセンブリをロードできないバグがあるから
            // そのためのワークアラウンド
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                var assembly = Assembly.Load(e.Name);
                if (assembly == null) throw new InvalidOperationException("invalid Assembly Name: " + e.Name);
                return assembly;
            };
            _domain = AppDomain.CreateDomain(Guid.NewGuid().ToString());

            var currentAssemblyPath = Assembly.GetExecutingAssembly().Location;
            _container = (PluginContainer)_domain.CreateInstanceFromAndUnwrap(
                currentAssemblyPath,
                "MayaCommandDebugPlugin.PluginContainer");

            string dllPath = null;
            try
            {
                dllPath = FindAssembly(nll);
            }
            catch (ArgumentException e)
            {
                MGlobal.displayError(e.Message);

                AppDomain.Unload(_domain);
                _domain = null;
                return "";
            }
            _container.LoadDll(dllPath);

            _container.InitializePlugin();

            _pluginName = nll.Split('.')[0];
            return _pluginName;
        }


        private object DoItImpl(string command, MArgList argl)
        {
            if (_domain == null)
            {
                MGlobal.displayError("the plugin contains " + command + " is already unloaded");
                return null;
            }

            var args = MArgListSerializer.Serialize(argl);
            return _container.doIt(command, args);
        }


        private string UnloadPluginImpl()
        {
            if (_domain == null)
            {
                MGlobal.displayInfo("the plugin is already unloaded");
                return "";
            }

            _container.UninitializePlugin();
            AppDomain.Unload(_domain);
            _domain = null;

            return _pluginName;
        }


        private string FindAssembly(string nll)
        {
            foreach(var path in Environment.GetEnvironmentVariable("MAYA_PLUG_IN_PATH").Split(Path.PathSeparator))
            {
                var dir = path.Replace("/", "\\");

                var dllPath = Path.Combine(dir, nll);
                if(File.Exists(dllPath)) return dllPath;

                dllPath = Path.Combine(dir, string.Format("{0}.dll", nll));
                if(File.Exists(dllPath)) return dllPath;

                dllPath = Path.Combine(dir, string.Format("{0}.nll.dll", nll));
                if(File.Exists(dllPath)) return dllPath;
            }

            throw new ArgumentException(string.Format("{0} is not found in %MAYA_PLUG_IN_PATH%", nll));
        }
    }

}
