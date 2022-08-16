// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "Validation of argument from context versus method parameter.", Scope = "member", Target = "~M:WebScheduler.Server.Interceptors.TenantValidationInterceptor.Invoke(Orleans.IIncomingGrainCallContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Roslynator", "RCS1102:Make class static.", Justification = "Can't be static because we use it as generic parameter for ILogger.", Scope = "type", Target = "~T:WebScheduler.Server.Program")]
