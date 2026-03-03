using System;
using System.Linq;
using System.Reflection;

var asmPath = @"D:\GitRep\AskMkiM\artifacts\tmpbuild\ICSharpCode.AvalonEdit.dll";
var asm = Assembly.LoadFrom(asmPath);
foreach (var t in asm.GetTypes().Where(t => t.Name.Contains("Margin")).OrderBy(t => t.FullName))
{
  var onRender = t.GetMethod("OnRender", BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.FlattenHierarchy);
  Console.WriteLine($"{t.FullName} | sealed={t.IsSealed} public={t.IsPublic || t.IsNestedPublic} base={t.BaseType?.FullName}");
  if (onRender != null)
    Console.WriteLine($"  OnRender declared in {onRender.DeclaringType?.Name} virtual={onRender.IsVirtual} final={onRender.IsFinal}");
}
