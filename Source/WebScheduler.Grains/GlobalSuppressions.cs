// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "Argument comes from Grain Call Request Context", Scope = "member", Target = "~M:WebScheduler.Grains.Scheduler.ScheduledTaskGrain.CreateAsync(WebScheduler.Abstractions.Grains.Scheduler.ScheduledTaskMetadata)~System.Threading.Tasks.ValueTask{WebScheduler.Abstractions.Grains.Scheduler.ScheduledTaskMetadata}")]
[assembly: SuppressMessage("Roslynator", "RCS1140:Add exception to documentation comment.", Justification = "Argument comes from Grain Call Request Context", Scope = "member", Target = "~M:WebScheduler.Grains.Scheduler.ScheduledTaskGrain.Invoke(Orleans.IIncomingGrainCallContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "Comes from grain context.", Scope = "member", Target = "~M:WebScheduler.Grains.Scheduler.ScheduledTaskGrain.Invoke(Orleans.IIncomingGrainCallContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "A logger should always be present in the Polly context.", Scope = "member", Target = "~M:Polly.ContextExtensions.GetLogger(Polly.Context)~Microsoft.Extensions.Logging.ILogger")]
[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "A grain key should always be present in the Polly context.", Scope = "member", Target = "~M:Polly.ContextExtensions.GetGrainKey(Polly.Context)~System.String")]
[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "Used for extension method discovery.", Scope = "namespace", Target = "~N:Orleans")]
