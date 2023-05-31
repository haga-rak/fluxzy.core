//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

export interface PolymorphicObject
{
	typeKind: string;
}
export interface DesktopErrorMessage
{
	message: string;
}
export interface FileOpeningRequestViewModel
{
	fileName: string;
}
export interface FileSaveViewModel
{
	fileName: string;
	fileSaveOption?: FileSaveOption;
}
export interface FileSaveOption
{
	saveOptionType: string;
	selectedExchangeIds?: number[];
}
export interface FullUrlSearchViewModel
{
	pattern: string;
}
export interface ExchangeContextInfo
{
	responseBodyText?: string;
	responseBodyLength?: number;
	isTextContent: boolean;
}
export interface FormattingResult
{
	title: string;
	errorMessage?: string;
	type: string;
}
export interface AuthorizationBasicResult extends FormattingResult
{
	clientId: string;
	clientSecret?: string;
}
export interface AuthorizationBearerResult extends FormattingResult
{
	token: string;
}
export interface AuthorizationResult extends FormattingResult
{
	value: string;
}
export interface QueryStringResult extends FormattingResult
{
	items: any[];
}
export interface RequestCookieResult extends FormattingResult
{
	cookies: any[];
}
export interface RequestJsonResult extends FormattingResult
{
	rawBody?: string;
	formattedBody: string;
}
export interface RawRequestHeaderResult extends FormattingResult
{
	rawHeader: string;
}
export interface RequestTextBodyResult extends FormattingResult
{
	text: string;
}
export interface RequestBodyAnalysisResult extends FormattingResult
{
	bodyLength: number;
	preferredFileName: string;
	contentType?: string;
}
export interface FormUrlEncodedResult extends FormattingResult
{
	items: FormUrlEncodedItem[];
}
export interface FormUrlEncodedItem
{
	key: string;
	value: string;
}
export interface MultipartFormContentResult extends FormattingResult
{
	items: MultipartItem[];
}
export interface MultipartItem
{
	name?: string;
	fileName?: string;
	contentType?: string;
	contentDisposition?: string;
	offset: number;
	length: number;
	rawHeader?: string;
	stringValue?: string;
}
export interface SaveFileMultipartActionModel
{
	filePath: string;
	offset: number;
	length: number;
}
export interface FormatterContainerViewModel
{
	requests: FormattingResult[];
	responses: FormattingResult[];
	contextInfo: ExchangeContextInfo;
}
export interface ResponseBodySummaryResult extends FormattingResult
{
	contentLength: number;
	compression: string;
	contentType?: string;
	bodyText?: string;
	preferredFileName: string;
}
export interface ResponseTextContentResult extends FormattingResult
{
}
export interface ResponseJsonResult extends FormattingResult
{
	formattedContent: string;
}
export interface SetCookieResult extends FormattingResult
{
	cookies: SetCookieItem[];
}
export interface SetCookieItem
{
	name: string;
	value: string;
	domain?: string;
	path?: string;
	sameSite?: string;
	expired: Date;
	maxAge?: number;
	secure: boolean;
	httpOnly: boolean;
}
export interface WsMessageFormattingResult extends FormattingResult
{
	messages: WsMessage[];
}
export interface ImageResult extends FormattingResult
{
	contentType: string;
}
export interface WsMessage
{
	id: number;
	direction: string;
	opCode: number;
	length: number;
	writtenLength: number;
	data?: number[];
	dataString?: string;
	messageStart: Date;
	messageEnd: Date;
	frameCount: number;
}
export interface CommentUpdateModel
{
	comment: string;
	exchangeIds: number[];
}
export interface TagUpdateModel
{
	name: string;
}
export interface TagGlobalApplyModel
{
	exchangeIds: number[];
	tagIdentifiers: string[];
}
export interface CertificateOnStore
{
	thumbPrint: string;
	friendlyName: string;
}
export interface IPEndPoint
{
	addressFamily: number;
	address: string;
	port: number;
}
export interface NetworkInterfaceInfo
{
	ipAddress: string;
	interfaceName: string;
}
export interface Agent
{
	id: number;
	friendlyName: string;
}
export interface ClientError
{
	errorCode: number;
	message: string;
	exceptionMessage?: string;
}
export interface DescriptionInfo
{
	description: string;
}
export interface CurlCommandResult
{
	id: string;
	args: any[];
	fileName?: string;
	flatCmdArgs: string;
	flatBashArgs: string;
	flatCmdArgsWithProxy: string;
	flatBashArgsWithProxy: string;
}
export interface TemplateToolBarFilterModel
{
	quickFilters: Filter[];
	lastUsedFilters: Filter[];
	agentFilters: Filter[];
}
export interface FilterTemplate
{
	label: string;
	group: string;
	filter: Filter;
}
export interface StoredFilter
{
	storeLocation: string;
	filters: Filter[];
}
export interface AnyFilter extends Filter
{
	identifier: string;
	filterScope: number;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
	description?: string;
	default: AnyFilter;
}
export interface CommentSearchFilter extends StringFilter
{
	filterScope: number;
	autoGeneratedName: string;
}
export interface ConnectionFilter extends Filter
{
	filterScope: number;
	connectionId: number;
	shortName: string;
	autoGeneratedName: string;
}
export interface ExecFilter extends Filter
{
	filename: string;
	arguments: string;
	writeHeaderToEnv: boolean;
	filterScope: number;
}
export interface Filter extends PolymorphicObject
{
	identifier: string;
	inverted: boolean;
	filterScope: number;
	scopeId: number;
	autoGeneratedName: string;
	preMadeFilter: boolean;
	genericName: string;
	locked: boolean;
	shortName?: string;
	description?: string;
	friendlyName: string;
	category: string;
	common: boolean;
}
export interface FilterCollection extends Filter
{
	identifier: string;
	children: Filter[];
	operation: number;
	filterScope: number;
	autoGeneratedName: string;
	shortName: string;
	explicitShortName?: string;
	genericName: string;
}
export interface HasCommentFilter extends Filter
{
	filterScope: number;
	autoGeneratedName: string;
	preMadeFilter: boolean;
}
export interface HasTagFilter extends Filter
{
	filterScope: number;
	autoGeneratedName: string;
	preMadeFilter: boolean;
}
export interface HeaderFilter extends StringFilter
{
	headerName: string;
	autoGeneratedName: string;
}
export interface IpEgressFilter extends StringFilter
{
	filterScope: number;
	genericName: string;
	autoGeneratedName: string;
	shortName: string;
}
export interface IsWebSocketFilter extends Filter
{
	identifier: string;
	filterScope: number;
	genericName: string;
	shortName?: string;
	description?: string;
	preMadeFilter: boolean;
}
export interface NoFilter extends Filter
{
	filterScope: number;
	genericName: string;
	preMadeFilter: boolean;
}
export interface StringFilter extends Filter
{
	pattern: string;
	operation: string;
	caseSensitive: boolean;
	autoGeneratedName: string;
}
export interface TagContainsFilter extends Filter
{
	tag?: Tag;
	filterScope: number;
	autoGeneratedName: string;
}
export interface SearchTextFilter extends Filter
{
	filterScope: number;
	identifier: string;
	searchInRequestHeader: boolean;
	searchInResponseHeader: boolean;
	searchInRequestBody: boolean;
	searchInResponseBody: boolean;
	caseSensitive: boolean;
	pattern: string;
	autoGeneratedName: string;
	shortName: string;
	genericName: string;
}
export interface ContentTypeJsonFilter extends ResponseHeaderFilter
{
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
	common: boolean;
}
export interface ContentTypeXmlFilter extends ResponseHeaderFilter
{
	autoGeneratedName: string;
	identifier: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface CssStyleFilter extends ResponseHeaderFilter
{
	autoGeneratedName: string;
	identifier: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface FontFilter extends ResponseHeaderFilter
{
	autoGeneratedName: string;
	identifier: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface ImageFilter extends ResponseHeaderFilter
{
	autoGeneratedName: string;
	identifier: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface NetworkErrorFilter extends Filter
{
	identifier: string;
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface ResponseHeaderFilter extends HeaderFilter
{
	filterScope: number;
	shortName: string;
	genericName: string;
	autoGeneratedName: string;
}
export interface StatusCodeClientErrorFilter extends Filter
{
	identifier: string;
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface StatusCodeFilter extends Filter
{
	statusCodes: number[];
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface StatusCodeRedirectionFilter extends Filter
{
	identifier: string;
	filterScope: number;
	autoGeneratedName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface StatusCodeServerErrorFilter extends Filter
{
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface StatusCodeSuccessFilter extends Filter
{
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
	common: boolean;
}
export interface AbsoluteUriFilter extends StringFilter
{
	filterScope: number;
	autoGeneratedName: string;
	shortName: string;
	common: boolean;
}
export interface AgentFilter extends Filter
{
	agent?: Agent;
	filterScope: number;
	genericName: string;
	autoGeneratedName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface AgentLabelFilter extends StringFilter
{
	filterScope: number;
	genericName: string;
	autoGeneratedName: string;
	shortName: string;
}
export interface AuthorityFilter extends StringFilter
{
	port: number;
	filterScope: number;
	shortName?: string;
	autoGeneratedName: string;
	genericName: string;
	common: boolean;
}
export interface DeleteFilter extends MethodFilter
{
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface GetFilter extends MethodFilter
{
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface H11TrafficOnlyFilter extends Filter
{
	filterScope: number;
	genericName: string;
	autoGeneratedName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface H2TrafficOnlyFilter extends Filter
{
	filterScope: number;
	genericName: string;
	autoGeneratedName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface HasAnyCookieOnRequestFilter extends Filter
{
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface HasAuthorizationBearerFilter extends Filter
{
	filterScope: number;
	autoGeneratedName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface HasAuthorizationFilter extends Filter
{
	filterScope: number;
	autoGeneratedName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface HasCookieOnRequestFilter extends StringFilter
{
	name: string;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
	filterScope: number;
}
export interface HasRequestBodyFilter extends Filter
{
	filterScope: number;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface HostFilter extends StringFilter
{
	filterScope: number;
	shortName?: string;
	autoGeneratedName: string;
	genericName: string;
	common: boolean;
}
export interface JsonRequestFilter extends RequestHeaderFilter
{
	filterScope: number;
	genericName: string;
	autoGeneratedName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface MethodFilter extends StringFilter
{
	filterScope: number;
	shortName: string;
	autoGeneratedName: string;
	genericName: string;
}
export interface PatchFilter extends MethodFilter
{
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface PathFilter extends StringFilter
{
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
}
export interface PostFilter extends MethodFilter
{
	genericName: string;
	common: boolean;
	shortName: string;
	preMadeFilter: boolean;
}
export interface PutFilter extends MethodFilter
{
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface RequestHeaderFilter extends HeaderFilter
{
	filterScope: number;
	shortName: string;
	genericName: string;
	autoGeneratedName: string;
}
export interface Rule
{
	identifier: string;
	name?: string;
	filter: Filter;
	action: Action;
	order: number;
	inScope: boolean;
}
export interface RuleContainer
{
	rule: Rule;
	enabled: boolean;
}
export interface RuleImportSetting
{
	deleteExisting: boolean;
	yamlContent?: string;
	fileName?: string;
}
export interface RuleExportSetting
{
	rules: Rule[];
}
export interface Certificate
{
	retrieveMode: string;
	serialNumber?: string;
	thumbPrint?: string;
	pkcs12File?: string;
	pkcs12Password?: string;
}
export interface Action extends PolymorphicObject
{
	actionScope: number;
	scopeId: number;
	identifier: string;
	defaultDescription: string;
	description?: string;
	friendlyName: string;
}
export interface MultipleScopeAction extends Action
{
	runScope?: number;
	actionScope: number;
}
export interface AddRequestHeaderAction extends Action
{
	headerName: string;
	headerValue: string;
	actionScope: number;
	defaultDescription: string;
}
export interface AddResponseHeaderAction extends Action
{
	headerName: string;
	headerValue: string;
	actionScope: number;
	defaultDescription: string;
}
export interface ApplyCommentAction extends Action
{
	actionScope: number;
	comment?: string;
	defaultDescription: string;
}
export interface ApplyTagAction extends Action
{
	actionScope: number;
	tag?: Tag;
	defaultDescription: string;
}
export interface BreakPointAction extends Action
{
	exchangeContext?: any;
	actionScope: number;
	defaultDescription: string;
}
export interface ChangeRequestMethodAction extends Action
{
	newMethod: string;
	actionScope: number;
	defaultDescription: string;
}
export interface ChangeRequestPathAction extends Action
{
	newPath: string;
	actionScope: number;
	defaultDescription: string;
}
export interface DeleteRequestHeaderAction extends Action
{
	headerName: string;
	actionScope: number;
	defaultDescription: string;
}
export interface DeleteResponseHeaderAction extends Action
{
	headerName: string;
	actionScope: number;
	defaultDescription: string;
}
export interface FileAppendAction extends MultipleScopeAction
{
	filename: string;
	text?: string;
	encoding?: string;
	defaultDescription: string;
}
export interface ForceHttp11Action extends Action
{
	actionScope: number;
	defaultDescription: string;
}
export interface ForceHttp2Action extends Action
{
	actionScope: number;
	defaultDescription: string;
}
export interface ForceTlsVersionAction extends Action
{
	sslProtocols: string;
	actionScope: number;
	defaultDescription: string;
}
export interface ForwardAction extends Action
{
	url: string;
	actionScope: number;
	defaultDescription: string;
}
export interface MountCertificateAuthorityAction extends Action
{
	actionScope: number;
	defaultDescription: string;
}
export interface RemoveCacheAction extends Action
{
	actionScope: number;
	defaultDescription: string;
}
export interface SetClientCertificateAction extends Action
{
	clientCertificate: Certificate;
	actionScope: number;
	defaultDescription: string;
}
export interface SkipRemoteCertificateValidationAction extends Action
{
	actionScope: number;
	defaultDescription: string;
}
export interface SkipSslTunnelingAction extends Action
{
	actionScope: number;
	defaultDescription: string;
}
export interface SpoofDnsAction extends Action
{
	remoteHostIp?: string;
	remoteHostPort?: number;
	actionScope: number;
	defaultDescription: string;
}
export interface StdErrAction extends MultipleScopeAction
{
	text?: string;
	defaultDescription: string;
}
export interface StdOutAction extends MultipleScopeAction
{
	text?: string;
	actionScope: number;
	defaultDescription: string;
}
export interface UpdateRequestHeaderAction extends Action
{
	headerName: string;
	headerValue: string;
	actionScope: number;
	defaultDescription: string;
}
export interface UpdateResponseHeaderAction extends Action
{
	headerName: string;
	headerValue: string;
	actionScope: number;
	defaultDescription: string;
}
export interface MockedResponseAction extends Action
{
	response: any;
	actionScope: number;
	defaultDescription: string;
}
export interface ReplaceRequestBodyAction extends Action
{
	replacement?: any;
	actionScope: number;
	defaultDescription: string;
}
export interface LastOpenFileState
{
	items: LastOpenFileItem[];
}
export interface LastOpenFileItem
{
	fullPath: string;
	fileName: string;
	creationDate: Date;
}
export interface AppVersion
{
	global: string;
	fluxzyCore: string;
	fluxzyDesktop: string;
}
export interface UiState
{
	id: string;
	fileState: FileState;
	proxyState: ProxyState;
	systemProxyState: any;
	viewFilter: ViewFilter;
	templateToolBarFilterModel: TemplateToolBarFilterModel;
	activeRules: Rule[];
	toolBarFilters: ToolBarFilter[];
	settingsHolder: FluxzySettingsHolder;
	lastOpenFileState: LastOpenFileState;
	breakPointState: BreakPointState;
	captureEnabled: boolean;
	haltEnabled: boolean;
}
export interface UiSetting
{
	value: string;
}
export interface ForwardMessage
{
	type: string;
	payload: any;
}
export interface ProxyState
{
	boundConnections: ProxyEndPoint[];
	onError: boolean;
	message?: string;
	runSettings?: ProxyNetworkState;
}
export interface ProxyNetworkState
{
	sslConfig: string;
	rawCaptureMode: string;
}
export interface ProxyEndPoint
{
	address: string;
	port: number;
}
export interface ProxyBindPoint
{
	endPoint: any;
	default: boolean;
}
export interface ArchivingPolicy
{
	type: number;
	directory?: string;
	none: ArchivingPolicy;
}
export interface FileState
{
	identifier: string;
	workingDirectory: string;
	mappedFileFullPath?: string;
	mappedFileName?: string;
	unsaved: boolean;
	lastModification: Date;
	contentOperation: any;
}
export interface ViewFilter
{
	id: number;
	filter: Filter;
	sourceFilter: Filter;
	empty: boolean;
}
export interface ToolBarFilter
{
	shortName?: string;
	filter: Filter;
	description?: string;
}
export interface FilteredExchangeState
{
	exchanges: Set<number>;
}
export interface BreakPointState
{
	hasToPop: boolean;
	activeEntries: number;
	entries: BreakPointContextInfo[];
	anyEnabled: boolean;
	isCatching: boolean;
	anyPendingRequest: boolean;
	activeFilters: Filter[];
	pausedExchangeIds: number[];
	emptyEntries: BreakPointState;
}
export interface BreakPointContextInfo
{
	exchangeId: number;
	exchange: ExchangeInfo;
	connectionInfo?: ConnectionInfo;
	lastLocation: string;
	currentHit?: string;
	done: boolean;
	originFilter: Filter;
	stepInfos: BreakPointContextStepInfo[];
}
export interface BreakPointContextStepInfo
{
	locationIndex: number;
	location: string;
	stepName: string;
	status: string;
	internalAlterationModel?: any;
	model?: any;
}
export interface ConnectionSetupStepModel
{
	forceNewConnection: boolean;
	skipRemoteCertificateValidation: boolean;
	ipAddress?: string;
	port?: number;
	done: boolean;
}
export interface RequestSetupStepModel
{
	done: boolean;
	flatHeader?: string;
	fromFile: boolean;
	fileName?: string;
	contentBody?: string;
	contentType?: string;
	bodyLength: number;
}
export interface ResponseSetupStepModel
{
	done: boolean;
	flatHeader?: string;
	fromFile: boolean;
	fileName?: string;
	contentBody?: string;
	contentType?: string;
	bodyLength: number;
}
export interface FluxzySettingsHolder
{
	startupSetting: FluxzySetting;
	viewModel?: FluxzySettingViewModel;
}
export interface FluxzySetting
{
	boundPoints: ProxyBindPoint[];
	boundPointsDescription: string;
	verbose: boolean;
	connectionPerHost: number;
	serverProtocols: string;
	caCertificate: Certificate;
	certificateCacheDirectory: string;
	autoInstallCertificate: boolean;
	checkCertificateRevocation: boolean;
	disableCertificateCache: boolean;
	captureRawPacket: boolean;
	captureInterfaceName?: string;
	byPassHost: string[];
	byPassHostFlat: string;
	archivingPolicy: ArchivingPolicy;
	alterationRules: Rule[];
	saveFilter?: Filter;
	globalSkipSslDecryption: boolean;
	outOfProcCapture: boolean;
	useBouncyCastle: boolean;
}
export interface FluxzySettingViewModel
{
	port: number;
	addresses: string[];
	listenType: string;
}
export interface HttpArchiveSavingSetting
{
	default: HttpArchiveSavingSetting;
	policy: string;
	harLimitMaxBodyLength: number;
}
export interface CertificateValidationResult
{
	subjectName?: string;
	errors: ValidationError[];
}
export interface ExchangeState
{
	exchanges: ExchangeContainer[];
	startIndex: number;
	endIndex: number;
	totalCount: number;
}
export interface ExchangeBrowsingState
{
	startIndex: number;
	count: number;
	type: number;
}
export interface ExchangeContainer
{
	id: number;
	exchangeInfo: IExchangeLine;
}
export interface ConnectionContainer
{
	id: number;
	connectionInfo: ConnectionInfo;
}
export interface TrunkState
{
	exchanges: ExchangeContainer[];
	connections: ConnectionContainer[];
	agents: Agent[];
	maxExchangeId: number;
	maxConnectionId: number;
	exchangesIndexer: { [key:number]: number };
	connectionsIndexer: { [key:number]: number };
}
export interface ContextMenuAction
{
	id?: string;
	label?: string;
	isDivider: boolean;
	filter?: Filter;
	sourceFilter?: Filter;
}
export interface FileContentDelete
{
	identifiers: number[];
}
export interface ArchiveMetaInformation
{
	captureDate: Date;
	tags: Tag[];
	viewFilters: Filter[];
}
export interface Tag
{
	identifier: string;
	value: string;
}
export interface ExchangeInfo extends IExchangeLine
{
	connectionId: number;
	id: number;
	requestHeader: RequestHeaderInfo;
	responseHeader?: ResponseHeaderInfo;
	metrics: ExchangeMetrics;
	contentType?: string;
	received: number;
	sent: number;
	done: boolean;
	pending: boolean;
	httpVersion: string;
	fullUrl: string;
	knownAuthority: string;
	knownPort: number;
	secure: boolean;
	method: string;
	path: string;
	statusCode: number;
	egressIp?: string;
	comment?: string;
	tags: Tag[];
	isWebSocket: boolean;
	webSocketMessages?: WsMessage[];
	agent?: Agent;
	clientErrors: ClientError[];
}
export interface IExchangeLine
{
	id: number;
	connectionId: number;
	method: string;
	path: string;
	knownAuthority: string;
	knownPort: number;
	secure: boolean;
	statusCode: number;
	comment?: string;
	pending: boolean;
	contentType?: string;
	received: number;
	sent: number;
}
export interface RequestHeaderInfo
{
	method: string;
	scheme: string;
	path: string;
	authority: string;
	headers: HeaderFieldInfo[];
}
export interface ResponseHeaderInfo
{
	statusCode: number;
	headers: HeaderFieldInfo[];
}
export interface ExchangeMetrics
{
	reusingConnection: boolean;
	receivedFromProxy: Date;
	retrievingPool: Date;
	requestHeaderSending: Date;
	requestHeaderSent: Date;
	requestBodySent: Date;
	responseHeaderStart: Date;
	responseHeaderEnd: Date;
	responseBodyStart: Date;
	responseBodyEnd: Date;
	remoteClosed: Date;
	createCertStart: Date;
	createCertEnd: Date;
	errorInstant: Date;
	totalSent: number;
	totalReceived: number;
	requestHeaderLength: number;
	responseHeaderLength: number;
	localPort: number;
	localAddress?: string;
}
export interface ExchangeMetricInfo
{
	exchangeId: number;
	rawMetrics: ExchangeMetrics;
	available: boolean;
	queued?: number;
	dns?: number;
	tcpHandShake?: number;
	sslHandShake?: number;
	requestHeader?: number;
	requestBody?: number;
	waiting?: number;
	receivingHeader?: number;
	receivingBody?: number;
	overAllDuration?: number;
}
export interface HeaderFieldInfo
{
	name: string;
	value: string;
	forwarded: boolean;
}
export interface ConnectionInfo
{
	id: number;
	httpVersion?: string;
	authority: AuthorityInfo;
	sslInfo?: SslInfo;
	requestProcessed: number;
	dnsSolveStart: Date;
	dnsSolveEnd: Date;
	tcpConnectionOpening: Date;
	tcpConnectionOpened: Date;
	sslNegotiationStart: Date;
	sslNegotiationEnd: Date;
	localPort: number;
	localAddress?: string;
	remoteAddress?: string;
}
export interface AuthorityInfo
{
	hostName: string;
	port: number;
	secure: boolean;
}
export interface SslInfo
{
	sslProtocol: string;
	remoteCertificateIssuer?: string;
	remoteCertificateSubject?: string;
	localCertificateSubject?: string;
	localCertificateIssuer?: string;
	negotiatedApplicationProtocol: string;
	keyExchangeAlgorithm: string;
	hashAlgorithm: number;
	cipherAlgorithm: number;
	remoteCertificate?: number[];
	localCertificate?: number[];
}
export interface ValidationError
{
	message: string;
}
export interface HarExportRequest
{
	fileName: string;
	saveSetting: HttpArchiveSavingSetting;
	exchangeIds?: number[];
}
export interface SazExportRequest
{
	fileName: string;
	exchangeIds?: number[];
}
export interface CertificateWizardStatus
{
	installed: boolean;
	userExplicitlyRefused: boolean;
	certificateFriendlyName: string;
	ignoreStep: boolean;
}
export interface UiUserSetting
{
	startupWizardSettings: StartupWizardSettings;
}
export interface StartupWizardSettings
{
	noCertificateInstallExplicit: boolean;
}
export interface QuickActionResult
{
	actions: QuickAction[];
}
export interface QuickAction
{
	id: string;
	type: string;
	category: string;
	label: string;
	iconClass: string[];
	otherClasses: string[];
	needExchangeId: boolean;
	quickActionPayload: QuickActionPayload;
	keywords: string[];
}
export interface QuickActionPayload
{
	filter?: Filter;
	action?: Action;
}
export interface ContextualFilterResult
{
	contextualFilters: ContextualFilter[];
}
export interface ContextualFilter
{
	filter: Filter;
	weight: number;
}
