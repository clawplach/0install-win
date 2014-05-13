namespace ZeroInstall.Services.Solvers

open NanoByte.Common.Tasks
open ZeroInstall.Services.Feeds
open ZeroInstall.Store
open ZeroInstall.Store.Feeds
open ZeroInstall.Store.Implementations

type SatSolver(config : Config, feedManager : IFeedManager, store : IStore, handler : ITaskHandler) =
    interface ISolver with
        member this.Solve requirements = null
