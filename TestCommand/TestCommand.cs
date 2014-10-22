using System;
using Autodesk.Maya.OpenMaya;
using System.Reflection;

// This line is mandatory to declare a new command in Maya
// You need to change the last parameter without your own
// node name and unique ID
//#warning You may need to change the Command name before continuing, then remove this line.
[assembly: MPxCommandClass(typeof(TestCommand.TestCommand), "test")]

namespace TestCommand
{
    // This class is instantiated by Maya each time when a command 
    // is called by the user or a script.
    public class TestCommand : MPxCommand, IMPxCommand, IUndoMPxCommand
    {

        override public void doIt(MArgList argl)
        {
            // Put your command code here
            // ...

            MGlobal.displayInfo(MethodBase.GetCurrentMethod().Name);
            MGlobal.displayInfo(AppDomain.CurrentDomain.FriendlyName);

            //for (uint argIndex = 0; argIndex < argl.length; ++argIndex)
            //{
            //    MGlobal.displayInfo("arg" + argIndex.ToString() + ": " + argl.asString(argIndex));
            //}

            redoIt();
        }

        override public void redoIt()
        {
            // Put your redo code here
            // ...
            MGlobal.displayInfo(MethodBase.GetCurrentMethod().Name);


            setResult(1.0);
        }

        override public void undoIt()
        {
            // Put your undo code here
            // ...
            MGlobal.displayInfo(MethodBase.GetCurrentMethod().Name);

        }

    }

}

