// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

//
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Panda.Test.InMemory.Blocks.MemBlock.#Panda.Core.Internal.ICacheKeyed`1<Panda.Core.Blocks.BlockOffset>.CacheKey",Justification = "This is essentailly a renaming of the interface method. The CacheKey is accessible to sub classes via the Offset property.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Panda.VirtualDirectory.#System.Collections.Generic.IReadOnlyDictionary`2<System.String,Panda.VirtualNode>.ContainsKey(System.String)", Justification = "ContainsKey is accessible via Contains. The latter's name is more appropriate for the context.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Panda.VirtualDirectory.#System.Collections.Generic.IReadOnlyDictionary`2<System.String,Panda.VirtualNode>.TryGetValue(System.String,Panda.VirtualNode&)", Justification = "TryGetValue is accessible via TryGetNode. The latter's name is more appropriate for the context.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Panda.VirtualDirectory.#System.Collections.Generic.IReadOnlyDictionary`2<System.String,Panda.VirtualNode>.Keys", Justification = "Keys is accessible via ContentNames. The latter's name is more appropriate for the context.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Panda.VirtualDirectory.#System.Collections.Generic.IReadOnlyDictionary`2<System.String,Panda.VirtualNode>.Values", Justification = "The implementation is accessible via GetEnumerator.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Panda.Core.IO.RawBlock.#Panda.Core.Internal.ICacheKeyed`1<Panda.Core.Blocks.BlockOffset>.CacheKey", Justification = "This is essentailly a renaming of the interface method. The CacheKey is accessible to sub classes via the Offset property.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2217:DoNotMarkEnumsWithFlags", Scope = "type", Target = "Panda.Core.IO.MemoryMapped.EIoControlCode",Justification = "E_IO_CONTROL_CODE values are defined by the windows API. We are not in control of which values/bits make sense.")]
