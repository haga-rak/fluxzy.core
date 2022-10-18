//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

export interface PolymorphicObject
{
	typeKind: string;
}
export interface FileSaveViewModel
{
	fileName: string;
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
export interface TemplateToolBarFilterModel
{
	quickFilters: Filter[];
	lastUsedFilters: Filter[];
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
export interface Filter extends PolymorphicObject
{
	identifier: string;
	filterScope: number;
	genericName: string;
	shortName?: string;
	preMadeFilter: boolean;
	description?: string;
	inverted: boolean;
	autoGeneratedName: string;
	locked: boolean;
	friendlyName: string;
}
export interface FilterCollection extends Filter
{
	children: Filter[];
	operation: number;
	filterScope: number;
	autoGeneratedName: string;
	shortName: string;
	explicitShortName?: string;
	genericName: string;
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
	shortName: string;
}
export interface NoFilter extends Filter
{
	filterScope: number;
	genericName: string;
	preMadeFilter: boolean;
}
export interface StringFilter extends Filter
{
	autoGeneratedName: string;
	pattern: string;
	operation: string;
	caseSensitive: boolean;
}
export interface ContentTypeJsonFilter extends ResponseHeaderFilter
{
	autoGeneratedName: string;
	identifier: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface ContentTypeXmlFilter extends ResponseHeaderFilter
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
export interface ResponseHeaderFilter extends HeaderFilter
{
	genericName: string;
	shortName: string;
	filterScope: number;
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
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface StatusCodeServerErrorFilter extends Filter
{
	identifier: string;
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface StatusCodeSuccessFilter extends Filter
{
	identifier: string;
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
	shortName: string;
	preMadeFilter: boolean;
}
export interface FullUrlFilter extends StringFilter
{
	filterScope: number;
	autoGeneratedName: string;
	shortName: string;
	genericName: string;
}
export interface HostFilter extends StringFilter
{
	filterScope: number;
	shortName?: string;
	autoGeneratedName: string;
	genericName: string;
}
export interface MethodFilter extends StringFilter
{
	identifier: string;
	filterScope: number;
	shortName: string;
	autoGeneratedName: string;
	genericName: string;
}
export interface PathFilter extends StringFilter
{
	filterScope: number;
	autoGeneratedName: string;
	genericName: string;
}
export interface RequestHeaderFilter extends HeaderFilter
{
	filterScope: number;
	shortName: string;
	genericName: string;
}
export interface UiState
{
	id: string;
	fileState: FileState;
	proxyState: ProxyState;
	systemProxyState: any;
	viewFilter: ViewFilter;
	templateToolBarFilterModel: TemplateToolBarFilterModel;
	toolBarFilters: ToolBarFilter[];
	settingsHolder: FluxzySettingsHolder;
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
	filter: Filter;
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
export interface FluxzySettingsHolder
{
	startupSetting: FluxzySetting;
}
export interface FluxzySetting
{
	boundPoints: ProxyBindPoint[];
	boundPointsDescription: string;
	verbose: boolean;
	connectionPerHost: number;
	serverProtocols: number;
	caCertificate: any;
	certificateCacheDirectory: string;
	autoInstallCertificate: boolean;
	checkCertificateRevocation: boolean;
	disableCertificateCache: boolean;
	byPassHost: string[];
	maxHeaderLength: number;
	archivingPolicy: ArchivingPolicy;
	alterationRules: any[];
	filterSetting: any;
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
	exchangeInfo: ExchangeInfo;
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
export interface ExchangeInfo
{
	id: number;
	connectionId: number;
	httpVersion: string;
	requestHeader: RequestHeaderInfo;
	responseHeader?: ResponseHeaderInfo;
	metrics: ExchangeMetrics;
	fullUrl: string;
	knownAuthority: string;
	method: string;
	path: string;
	contentType?: string;
	done: boolean;
	statusCode: number;
	egressIp?: string;
	comment?: string;
	tags: Tag[];
	pending: boolean;
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
	totalSent: number;
	totalReceived: number;
	localPort: number;
	localAddress?: string;
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
	sslProtocol: number;
	remoteCertificateIssuer?: string;
	remoteCertificateSubject?: string;
	localCertificateSubject?: string;
	localCertificateIssuer?: string;
	negotiatedApplicationProtocol: string;
	keyExchangeAlgorithm: string;
	hashAlgorithm: number;
	cipherAlgorithm: number;
}
