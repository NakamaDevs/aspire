addElixirApp(name: string, appDirectory: string): ElixirAppResourcePromise;
addPhoenixApp(name: string, appDirectory: string): PhoenixAppResourcePromise;
addMixRelease(name: string, releaseDirectory: string, options?: AddMixReleaseOptions): MixReleaseResourcePromise;
addElixirApp(name: string, appDirectory: string): ElixirAppResourcePromise;
addPhoenixApp(name: string, appDirectory: string): PhoenixAppResourcePromise;
addMixRelease(name: string, releaseDirectory: string, options?: AddMixReleaseOptions): MixReleaseResourcePromise;
async _addElixirAppInternal(name: string, appDirectory: string): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/addElixirApp',
addElixirApp(name: string, appDirectory: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._addElixirAppInternal(name, appDirectory), this._client);
async _addPhoenixAppInternal(name: string, appDirectory: string): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/addPhoenixApp',
addPhoenixApp(name: string, appDirectory: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._addPhoenixAppInternal(name, appDirectory), this._client);
async _addMixReleaseInternal(name: string, releaseDirectory: string, releaseName?: string): Promise<MixReleaseResource> {
'Aspire.Hosting.Elixir/addMixRelease',
addMixRelease(name: string, releaseDirectory: string, options?: AddMixReleaseOptions): MixReleaseResourcePromise {
return new MixReleaseResourcePromiseImpl(this._addMixReleaseInternal(name, releaseDirectory, releaseName), this._client);
addElixirApp(name: string, appDirectory: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.addElixirApp(name, appDirectory)), this._client);
addPhoenixApp(name: string, appDirectory: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.addPhoenixApp(name, appDirectory)), this._client);
addMixRelease(name: string, releaseDirectory: string, options?: AddMixReleaseOptions): MixReleaseResourcePromise {
return new MixReleaseResourcePromiseImpl(this._promise.then(obj => obj.addMixRelease(name, releaseDirectory, options)), this._client);
withLiveReload(options?: WithLiveReloadOptions): ElixirAppResourcePromise;
withAppArgs(args: any[]): ElixirAppResourcePromise;
withMixDeps(options?: WithMixDepsOptions): ElixirAppResourcePromise;
withMixCompile(): ElixirAppResourcePromise;
withMixEnv(env: string): ElixirAppResourcePromise;
withMixTask(task: string, args: any[]): ElixirAppResourcePromise;
withErlFlags(flags: string): ElixirAppResourcePromise;
withElixirErlOptions(options: string): ElixirAppResourcePromise;
withNodeName(name: string, options?: WithNodeNameOptions): ElixirAppResourcePromise;
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): ElixirAppResourcePromise;
withEctoMigrate(): ElixirAppResourcePromise;
withReleaseName(name: string): ElixirAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): ElixirAppResourcePromise;
withAppArgs(args: any[]): ElixirAppResourcePromise;
withMixDeps(options?: WithMixDepsOptions): ElixirAppResourcePromise;
withMixCompile(): ElixirAppResourcePromise;
withMixEnv(env: string): ElixirAppResourcePromise;
withMixTask(task: string, args: any[]): ElixirAppResourcePromise;
withErlFlags(flags: string): ElixirAppResourcePromise;
withElixirErlOptions(options: string): ElixirAppResourcePromise;
withNodeName(name: string, options?: WithNodeNameOptions): ElixirAppResourcePromise;
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): ElixirAppResourcePromise;
withEctoMigrate(): ElixirAppResourcePromise;
withReleaseName(name: string): ElixirAppResourcePromise;
private async _withLiveReloadInternal(enabled?: boolean): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withLiveReload',
withLiveReload(options?: WithLiveReloadOptions): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withLiveReloadInternal(enabled), this._client);
private async _withAppArgsInternal(args: any[]): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withAppArgs',
withAppArgs(args: any[]): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withAppArgsInternal(args), this._client);
private async _withMixDepsInternal(install?: boolean): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withMixDeps',
withMixDeps(options?: WithMixDepsOptions): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withMixDepsInternal(install), this._client);
private async _withMixCompileInternal(): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withMixCompile',
withMixCompile(): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withMixCompileInternal(), this._client);
private async _withMixEnvInternal(env: string): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withMixEnv',
withMixEnv(env: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withMixEnvInternal(env), this._client);
private async _withMixTaskInternal(task: string, args: any[]): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withMixTask',
withMixTask(task: string, args: any[]): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withMixTaskInternal(task, args), this._client);
private async _withErlFlagsInternal(flags: string): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withErlFlags',
withErlFlags(flags: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withErlFlagsInternal(flags), this._client);
private async _withElixirErlOptionsInternal(options: string): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withElixirErlOptions',
withElixirErlOptions(options: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withElixirErlOptionsInternal(options), this._client);
private async _withNodeNameInternal(name: string, cookie?: Awaitable<ParameterResource>): Promise<ElixirAppResource> {
withNodeName(name: string, options?: WithNodeNameOptions): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withNodeNameInternal(name, cookie), this._client);
private async _withEctoDatabaseInternal(database: Awaitable<ResourceWithConnectionString>): Promise<ElixirAppResource> {
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withEctoDatabaseInternal(database), this._client);
private async _withEctoMigrateInternal(): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withEctoMigrate',
withEctoMigrate(): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withEctoMigrateInternal(), this._client);
private async _withReleaseNameInternal(name: string): Promise<ElixirAppResource> {
'Aspire.Hosting.Elixir/withReleaseName',
withReleaseName(name: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._withReleaseNameInternal(name), this._client);
withLiveReload(options?: WithLiveReloadOptions): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withLiveReload(options)), this._client);
withAppArgs(args: any[]): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withAppArgs(args)), this._client);
withMixDeps(options?: WithMixDepsOptions): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withMixDeps(options)), this._client);
withMixCompile(): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withMixCompile()), this._client);
withMixEnv(env: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withMixEnv(env)), this._client);
withMixTask(task: string, args: any[]): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withMixTask(task, args)), this._client);
withErlFlags(flags: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withErlFlags(flags)), this._client);
withElixirErlOptions(options: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withElixirErlOptions(options)), this._client);
withNodeName(name: string, options?: WithNodeNameOptions): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withNodeName(name, options)), this._client);
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withEctoDatabase(database)), this._client);
withEctoMigrate(): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withEctoMigrate()), this._client);
withReleaseName(name: string): ElixirAppResourcePromise {
return new ElixirAppResourcePromiseImpl(this._promise.then(obj => obj.withReleaseName(name)), this._client);
withLiveReload(options?: WithLiveReloadOptions): PhoenixAppResourcePromise;
withAppArgs(args: any[]): PhoenixAppResourcePromise;
withMixDeps(options?: WithMixDepsOptions): PhoenixAppResourcePromise;
withMixCompile(): PhoenixAppResourcePromise;
withMixEnv(env: string): PhoenixAppResourcePromise;
withMixTask(task: string, args: any[]): PhoenixAppResourcePromise;
withErlFlags(flags: string): PhoenixAppResourcePromise;
withElixirErlOptions(options: string): PhoenixAppResourcePromise;
withNodeName(name: string, options?: WithNodeNameOptions): PhoenixAppResourcePromise;
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): PhoenixAppResourcePromise;
withEctoMigrate(): PhoenixAppResourcePromise;
withReleaseName(name: string): PhoenixAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): PhoenixAppResourcePromise;
withAppArgs(args: any[]): PhoenixAppResourcePromise;
withMixDeps(options?: WithMixDepsOptions): PhoenixAppResourcePromise;
withMixCompile(): PhoenixAppResourcePromise;
withMixEnv(env: string): PhoenixAppResourcePromise;
withMixTask(task: string, args: any[]): PhoenixAppResourcePromise;
withErlFlags(flags: string): PhoenixAppResourcePromise;
withElixirErlOptions(options: string): PhoenixAppResourcePromise;
withNodeName(name: string, options?: WithNodeNameOptions): PhoenixAppResourcePromise;
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): PhoenixAppResourcePromise;
withEctoMigrate(): PhoenixAppResourcePromise;
withReleaseName(name: string): PhoenixAppResourcePromise;
private async _withLiveReloadInternal(enabled?: boolean): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withLiveReload',
withLiveReload(options?: WithLiveReloadOptions): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withLiveReloadInternal(enabled), this._client);
private async _withAppArgsInternal(args: any[]): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withAppArgs',
withAppArgs(args: any[]): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withAppArgsInternal(args), this._client);
private async _withMixDepsInternal(install?: boolean): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withMixDeps',
withMixDeps(options?: WithMixDepsOptions): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withMixDepsInternal(install), this._client);
private async _withMixCompileInternal(): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withMixCompile',
withMixCompile(): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withMixCompileInternal(), this._client);
private async _withMixEnvInternal(env: string): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withMixEnv',
withMixEnv(env: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withMixEnvInternal(env), this._client);
private async _withMixTaskInternal(task: string, args: any[]): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withMixTask',
withMixTask(task: string, args: any[]): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withMixTaskInternal(task, args), this._client);
private async _withErlFlagsInternal(flags: string): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withErlFlags',
withErlFlags(flags: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withErlFlagsInternal(flags), this._client);
private async _withElixirErlOptionsInternal(options: string): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withElixirErlOptions',
withElixirErlOptions(options: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withElixirErlOptionsInternal(options), this._client);
private async _withNodeNameInternal(name: string, cookie?: Awaitable<ParameterResource>): Promise<PhoenixAppResource> {
withNodeName(name: string, options?: WithNodeNameOptions): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withNodeNameInternal(name, cookie), this._client);
private async _withEctoDatabaseInternal(database: Awaitable<ResourceWithConnectionString>): Promise<PhoenixAppResource> {
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withEctoDatabaseInternal(database), this._client);
private async _withEctoMigrateInternal(): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withEctoMigrate',
withEctoMigrate(): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withEctoMigrateInternal(), this._client);
private async _withReleaseNameInternal(name: string): Promise<PhoenixAppResource> {
'Aspire.Hosting.Elixir/withReleaseName',
withReleaseName(name: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._withReleaseNameInternal(name), this._client);
withLiveReload(options?: WithLiveReloadOptions): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withLiveReload(options)), this._client);
withAppArgs(args: any[]): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withAppArgs(args)), this._client);
withMixDeps(options?: WithMixDepsOptions): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withMixDeps(options)), this._client);
withMixCompile(): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withMixCompile()), this._client);
withMixEnv(env: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withMixEnv(env)), this._client);
withMixTask(task: string, args: any[]): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withMixTask(task, args)), this._client);
withErlFlags(flags: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withErlFlags(flags)), this._client);
withElixirErlOptions(options: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withElixirErlOptions(options)), this._client);
withNodeName(name: string, options?: WithNodeNameOptions): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withNodeName(name, options)), this._client);
withEctoDatabase(database: Awaitable<ResourceWithConnectionString>): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withEctoDatabase(database)), this._client);
withEctoMigrate(): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withEctoMigrate()), this._client);
withReleaseName(name: string): PhoenixAppResourcePromise {
return new PhoenixAppResourcePromiseImpl(this._promise.then(obj => obj.withReleaseName(name)), this._client);