addDartApp(name: string, appDirectory: string, options?: AddDartAppOptions): DartAppResourcePromise;
addJasprApp(name: string, appDirectory: string): JasprAppResourcePromise;
addServerpodApp(name: string, serverDirectory: string): ServerpodAppResourcePromise;
addDartApp(name: string, appDirectory: string, options?: AddDartAppOptions): DartAppResourcePromise;
addJasprApp(name: string, appDirectory: string): JasprAppResourcePromise;
addServerpodApp(name: string, serverDirectory: string): ServerpodAppResourcePromise;
async _addDartAppInternal(name: string, appDirectory: string, entrypoint?: string): Promise<DartAppResource> {
'Aspire.Hosting.Dart/addDartApp',
addDartApp(name: string, appDirectory: string, options?: AddDartAppOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._addDartAppInternal(name, appDirectory, entrypoint), this._client);
async _addJasprAppInternal(name: string, appDirectory: string): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/addJasprApp',
addJasprApp(name: string, appDirectory: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._addJasprAppInternal(name, appDirectory), this._client);
async _addServerpodAppInternal(name: string, serverDirectory: string): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/addServerpodApp',
addServerpodApp(name: string, serverDirectory: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._addServerpodAppInternal(name, serverDirectory), this._client);
addDartApp(name: string, appDirectory: string, options?: AddDartAppOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.addDartApp(name, appDirectory, options)), this._client);
addJasprApp(name: string, appDirectory: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.addJasprApp(name, appDirectory)), this._client);
addServerpodApp(name: string, serverDirectory: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.addServerpodApp(name, serverDirectory)), this._client);
withEntrypoint(entrypoint: string): ContainerResourcePromise;
withEntrypoint(entrypoint: string): ContainerResourcePromise;
private async _withEntrypointInternal(entrypoint: string): Promise<ContainerResource> {
'Aspire.Hosting/withEntrypoint',
withEntrypoint(entrypoint: string): ContainerResourcePromise {
return new ContainerResourcePromiseImpl(this._withEntrypointInternal(entrypoint), this._client);
withEntrypoint(entrypoint: string): ContainerResourcePromise {
return new ContainerResourcePromiseImpl(this._promise.then(obj => obj.withEntrypoint(entrypoint)), this._client);
withRunCommand(command: string, args: any[]): DartAppResourcePromise;
withEntrypoint(entrypoint: string): DartAppResourcePromise;
withDartRunArgs(args: string[]): DartAppResourcePromise;
withVmService(options?: WithVmServiceOptions): DartAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): DartAppResourcePromise;
withPublishEntrypoint(entrypoint: string): DartAppResourcePromise;
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): DartAppResourcePromise;
withStaticSiteTool(package: string): DartAppResourcePromise;
withAppArgs(args: any[]): DartAppResourcePromise;
withDartDefine(key: string, value: string): DartAppResourcePromise;
withPubGet(options?: WithPubGetOptions): DartAppResourcePromise;
withRunCommand(command: string, args: any[]): DartAppResourcePromise;
withEntrypoint(entrypoint: string): DartAppResourcePromise;
withDartRunArgs(args: string[]): DartAppResourcePromise;
withVmService(options?: WithVmServiceOptions): DartAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): DartAppResourcePromise;
withPublishEntrypoint(entrypoint: string): DartAppResourcePromise;
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): DartAppResourcePromise;
withStaticSiteTool(package: string): DartAppResourcePromise;
withAppArgs(args: any[]): DartAppResourcePromise;
withDartDefine(key: string, value: string): DartAppResourcePromise;
withPubGet(options?: WithPubGetOptions): DartAppResourcePromise;
private async _withRunCommandInternal(command: string, args: any[]): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withRunCommand',
withRunCommand(command: string, args: any[]): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withRunCommandInternal(command, args), this._client);
private async _withEntrypointInternal(entrypoint: string): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withEntrypoint',
withEntrypoint(entrypoint: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withEntrypointInternal(entrypoint), this._client);
private async _withDartRunArgsInternal(args: string[]): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withDartRunArgs',
withDartRunArgs(args: string[]): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withDartRunArgsInternal(args), this._client);
private async _withVmServiceInternal(port?: number): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withVmService',
withVmService(options?: WithVmServiceOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withVmServiceInternal(port), this._client);
private async _withLiveReloadInternal(enabled?: boolean): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withLiveReload',
withLiveReload(options?: WithLiveReloadOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withLiveReloadInternal(enabled), this._client);
private async _withPublishEntrypointInternal(entrypoint: string): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withPublishEntrypoint',
withPublishEntrypoint(entrypoint: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withPublishEntrypointInternal(entrypoint), this._client);
private async _withStaticSiteBuildInternal(command: string, args: string[], outputDirectory: string, spaFallback?: boolean, buildImage?: string): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withStaticSiteBuild',
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withStaticSiteBuildInternal(command, args, outputDirectory, spaFallback, buildImage), this._client);
private async _withStaticSiteToolInternal(package: string): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withStaticSiteTool',
withStaticSiteTool(package: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withStaticSiteToolInternal(package), this._client);
private async _withAppArgsInternal(args: any[]): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withAppArgs',
withAppArgs(args: any[]): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withAppArgsInternal(args), this._client);
private async _withDartDefineInternal(key: string, value: string): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withDartDefine',
withDartDefine(key: string, value: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withDartDefineInternal(key, value), this._client);
private async _withPubGetInternal(install?: boolean): Promise<DartAppResource> {
'Aspire.Hosting.Dart/withPubGet',
withPubGet(options?: WithPubGetOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._withPubGetInternal(install), this._client);
withRunCommand(command: string, args: any[]): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withRunCommand(command, args)), this._client);
withEntrypoint(entrypoint: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withEntrypoint(entrypoint)), this._client);
withDartRunArgs(args: string[]): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withDartRunArgs(args)), this._client);
withVmService(options?: WithVmServiceOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withVmService(options)), this._client);
withLiveReload(options?: WithLiveReloadOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withLiveReload(options)), this._client);
withPublishEntrypoint(entrypoint: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withPublishEntrypoint(entrypoint)), this._client);
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withStaticSiteBuild(command, args, outputDirectory, options)), this._client);
withStaticSiteTool(package: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withStaticSiteTool(package)), this._client);
withAppArgs(args: any[]): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withAppArgs(args)), this._client);
withDartDefine(key: string, value: string): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withDartDefine(key, value)), this._client);
withPubGet(options?: WithPubGetOptions): DartAppResourcePromise {
return new DartAppResourcePromiseImpl(this._promise.then(obj => obj.withPubGet(options)), this._client);
withRunCommand(command: string, args: any[]): JasprAppResourcePromise;
withEntrypoint(entrypoint: string): JasprAppResourcePromise;
withDartRunArgs(args: string[]): JasprAppResourcePromise;
withVmService(options?: WithVmServiceOptions): JasprAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): JasprAppResourcePromise;
withPublishEntrypoint(entrypoint: string): JasprAppResourcePromise;
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): JasprAppResourcePromise;
withStaticSiteTool(package: string): JasprAppResourcePromise;
withJasprMode(mode: JasprMode): JasprAppResourcePromise;
withJasprDevPorts(options?: WithJasprDevPortsOptions): JasprAppResourcePromise;
withAppArgs(args: any[]): JasprAppResourcePromise;
withDartDefine(key: string, value: string): JasprAppResourcePromise;
withPubGet(options?: WithPubGetOptions): JasprAppResourcePromise;
withRunCommand(command: string, args: any[]): JasprAppResourcePromise;
withEntrypoint(entrypoint: string): JasprAppResourcePromise;
withDartRunArgs(args: string[]): JasprAppResourcePromise;
withVmService(options?: WithVmServiceOptions): JasprAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): JasprAppResourcePromise;
withPublishEntrypoint(entrypoint: string): JasprAppResourcePromise;
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): JasprAppResourcePromise;
withStaticSiteTool(package: string): JasprAppResourcePromise;
withJasprMode(mode: JasprMode): JasprAppResourcePromise;
withJasprDevPorts(options?: WithJasprDevPortsOptions): JasprAppResourcePromise;
withAppArgs(args: any[]): JasprAppResourcePromise;
withDartDefine(key: string, value: string): JasprAppResourcePromise;
withPubGet(options?: WithPubGetOptions): JasprAppResourcePromise;
private async _withRunCommandInternal(command: string, args: any[]): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withRunCommand',
withRunCommand(command: string, args: any[]): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withRunCommandInternal(command, args), this._client);
private async _withEntrypointInternal(entrypoint: string): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withEntrypoint',
withEntrypoint(entrypoint: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withEntrypointInternal(entrypoint), this._client);
private async _withDartRunArgsInternal(args: string[]): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withDartRunArgs',
withDartRunArgs(args: string[]): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withDartRunArgsInternal(args), this._client);
private async _withVmServiceInternal(port?: number): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withVmService',
withVmService(options?: WithVmServiceOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withVmServiceInternal(port), this._client);
private async _withLiveReloadInternal(enabled?: boolean): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withLiveReload',
withLiveReload(options?: WithLiveReloadOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withLiveReloadInternal(enabled), this._client);
private async _withPublishEntrypointInternal(entrypoint: string): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withPublishEntrypoint',
withPublishEntrypoint(entrypoint: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withPublishEntrypointInternal(entrypoint), this._client);
private async _withStaticSiteBuildInternal(command: string, args: string[], outputDirectory: string, spaFallback?: boolean, buildImage?: string): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withStaticSiteBuild',
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withStaticSiteBuildInternal(command, args, outputDirectory, spaFallback, buildImage), this._client);
private async _withStaticSiteToolInternal(package: string): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withStaticSiteTool',
withStaticSiteTool(package: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withStaticSiteToolInternal(package), this._client);
private async _withJasprModeInternal(mode: JasprMode): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withJasprMode',
withJasprMode(mode: JasprMode): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withJasprModeInternal(mode), this._client);
private async _withJasprDevPortsInternal(webPort?: number, proxyPort?: number): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withJasprDevPorts',
withJasprDevPorts(options?: WithJasprDevPortsOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withJasprDevPortsInternal(webPort, proxyPort), this._client);
private async _withAppArgsInternal(args: any[]): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withAppArgs',
withAppArgs(args: any[]): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withAppArgsInternal(args), this._client);
private async _withDartDefineInternal(key: string, value: string): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withDartDefine',
withDartDefine(key: string, value: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withDartDefineInternal(key, value), this._client);
private async _withPubGetInternal(install?: boolean): Promise<JasprAppResource> {
'Aspire.Hosting.Dart/withPubGet',
withPubGet(options?: WithPubGetOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._withPubGetInternal(install), this._client);
withRunCommand(command: string, args: any[]): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withRunCommand(command, args)), this._client);
withEntrypoint(entrypoint: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withEntrypoint(entrypoint)), this._client);
withDartRunArgs(args: string[]): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withDartRunArgs(args)), this._client);
withVmService(options?: WithVmServiceOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withVmService(options)), this._client);
withLiveReload(options?: WithLiveReloadOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withLiveReload(options)), this._client);
withPublishEntrypoint(entrypoint: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withPublishEntrypoint(entrypoint)), this._client);
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withStaticSiteBuild(command, args, outputDirectory, options)), this._client);
withStaticSiteTool(package: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withStaticSiteTool(package)), this._client);
withJasprMode(mode: JasprMode): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withJasprMode(mode)), this._client);
withJasprDevPorts(options?: WithJasprDevPortsOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withJasprDevPorts(options)), this._client);
withAppArgs(args: any[]): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withAppArgs(args)), this._client);
withDartDefine(key: string, value: string): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withDartDefine(key, value)), this._client);
withPubGet(options?: WithPubGetOptions): JasprAppResourcePromise {
return new JasprAppResourcePromiseImpl(this._promise.then(obj => obj.withPubGet(options)), this._client);
withRunCommand(command: string, args: any[]): ServerpodAppResourcePromise;
withEntrypoint(entrypoint: string): ServerpodAppResourcePromise;
withDartRunArgs(args: string[]): ServerpodAppResourcePromise;
withVmService(options?: WithVmServiceOptions): ServerpodAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): ServerpodAppResourcePromise;
withPublishEntrypoint(entrypoint: string): ServerpodAppResourcePromise;
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): ServerpodAppResourcePromise;
withStaticSiteTool(package: string): ServerpodAppResourcePromise;
withServerpodDatabase(database: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise;
withServerpodRedis(cache: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise;
withServerpodMode(mode: string): ServerpodAppResourcePromise;
withApplyMigrations(options?: WithApplyMigrationsOptions): ServerpodAppResourcePromise;
withAppArgs(args: any[]): ServerpodAppResourcePromise;
withDartDefine(key: string, value: string): ServerpodAppResourcePromise;
withPubGet(options?: WithPubGetOptions): ServerpodAppResourcePromise;
withRunCommand(command: string, args: any[]): ServerpodAppResourcePromise;
withEntrypoint(entrypoint: string): ServerpodAppResourcePromise;
withDartRunArgs(args: string[]): ServerpodAppResourcePromise;
withVmService(options?: WithVmServiceOptions): ServerpodAppResourcePromise;
withLiveReload(options?: WithLiveReloadOptions): ServerpodAppResourcePromise;
withPublishEntrypoint(entrypoint: string): ServerpodAppResourcePromise;
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): ServerpodAppResourcePromise;
withStaticSiteTool(package: string): ServerpodAppResourcePromise;
withServerpodDatabase(database: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise;
withServerpodRedis(cache: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise;
withServerpodMode(mode: string): ServerpodAppResourcePromise;
withApplyMigrations(options?: WithApplyMigrationsOptions): ServerpodAppResourcePromise;
withAppArgs(args: any[]): ServerpodAppResourcePromise;
withDartDefine(key: string, value: string): ServerpodAppResourcePromise;
withPubGet(options?: WithPubGetOptions): ServerpodAppResourcePromise;
private async _withRunCommandInternal(command: string, args: any[]): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withRunCommand',
withRunCommand(command: string, args: any[]): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withRunCommandInternal(command, args), this._client);
private async _withEntrypointInternal(entrypoint: string): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withEntrypoint',
withEntrypoint(entrypoint: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withEntrypointInternal(entrypoint), this._client);
private async _withDartRunArgsInternal(args: string[]): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withDartRunArgs',
withDartRunArgs(args: string[]): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withDartRunArgsInternal(args), this._client);
private async _withVmServiceInternal(port?: number): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withVmService',
withVmService(options?: WithVmServiceOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withVmServiceInternal(port), this._client);
private async _withLiveReloadInternal(enabled?: boolean): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withLiveReload',
withLiveReload(options?: WithLiveReloadOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withLiveReloadInternal(enabled), this._client);
private async _withPublishEntrypointInternal(entrypoint: string): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withPublishEntrypoint',
withPublishEntrypoint(entrypoint: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withPublishEntrypointInternal(entrypoint), this._client);
private async _withStaticSiteBuildInternal(command: string, args: string[], outputDirectory: string, spaFallback?: boolean, buildImage?: string): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withStaticSiteBuild',
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withStaticSiteBuildInternal(command, args, outputDirectory, spaFallback, buildImage), this._client);
private async _withStaticSiteToolInternal(package: string): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withStaticSiteTool',
withStaticSiteTool(package: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withStaticSiteToolInternal(package), this._client);
private async _withServerpodDatabaseInternal(database: Awaitable<ResourceWithConnectionString>): Promise<ServerpodAppResource> {
withServerpodDatabase(database: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withServerpodDatabaseInternal(database), this._client);
private async _withServerpodRedisInternal(cache: Awaitable<ResourceWithConnectionString>): Promise<ServerpodAppResource> {
withServerpodRedis(cache: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withServerpodRedisInternal(cache), this._client);
private async _withServerpodModeInternal(mode: string): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withServerpodMode',
withServerpodMode(mode: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withServerpodModeInternal(mode), this._client);
private async _withApplyMigrationsInternal(applyMigrations?: boolean): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withApplyMigrations',
withApplyMigrations(options?: WithApplyMigrationsOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withApplyMigrationsInternal(applyMigrations), this._client);
private async _withAppArgsInternal(args: any[]): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withAppArgs',
withAppArgs(args: any[]): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withAppArgsInternal(args), this._client);
private async _withDartDefineInternal(key: string, value: string): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withDartDefine',
withDartDefine(key: string, value: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withDartDefineInternal(key, value), this._client);
private async _withPubGetInternal(install?: boolean): Promise<ServerpodAppResource> {
'Aspire.Hosting.Dart/withPubGet',
withPubGet(options?: WithPubGetOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._withPubGetInternal(install), this._client);
withRunCommand(command: string, args: any[]): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withRunCommand(command, args)), this._client);
withEntrypoint(entrypoint: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withEntrypoint(entrypoint)), this._client);
withDartRunArgs(args: string[]): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withDartRunArgs(args)), this._client);
withVmService(options?: WithVmServiceOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withVmService(options)), this._client);
withLiveReload(options?: WithLiveReloadOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withLiveReload(options)), this._client);
withPublishEntrypoint(entrypoint: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withPublishEntrypoint(entrypoint)), this._client);
withStaticSiteBuild(command: string, args: string[], outputDirectory: string, options?: WithStaticSiteBuildOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withStaticSiteBuild(command, args, outputDirectory, options)), this._client);
withStaticSiteTool(package: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withStaticSiteTool(package)), this._client);
withServerpodDatabase(database: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withServerpodDatabase(database)), this._client);
withServerpodRedis(cache: Awaitable<ResourceWithConnectionString>): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withServerpodRedis(cache)), this._client);
withServerpodMode(mode: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withServerpodMode(mode)), this._client);
withApplyMigrations(options?: WithApplyMigrationsOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withApplyMigrations(options)), this._client);
withAppArgs(args: any[]): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withAppArgs(args)), this._client);
withDartDefine(key: string, value: string): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withDartDefine(key, value)), this._client);
withPubGet(options?: WithPubGetOptions): ServerpodAppResourcePromise {
return new ServerpodAppResourcePromiseImpl(this._promise.then(obj => obj.withPubGet(options)), this._client);