using System;
using System.Linq;
using System.Reflection;

var asmPath = @"D:\GitRep\AskMkiM\artifacts\tmpbuild\ICSharpCode.AvalonEdit.dll";
var asm = Assembly.LoadFrom(asmPath);
var types = asm.GetTypes().Where(t => t.Name.Contains("Margin", StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.FullName);
foreach (var t in types)
{
    var onRender = t.GetMethod("OnRender", BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.FlattenHierarchy);
    var isPublic = t.IsPublic || t.IsNestedPublic;
    Console.WriteLine(t.FullName + " | public=" + isPublic + " sealed=" + t.IsSealed + " abstract=" + t.IsAbstract + " base=" + t.BaseType?.FullName);
    if (onRender != null)
    {
        Console.WriteLine("  OnRender declaring=" + onRender.DeclaringType?.FullName + " virtual=" + onRender.IsVirtual + " final=" + onRender.IsFinal);
    }
}
