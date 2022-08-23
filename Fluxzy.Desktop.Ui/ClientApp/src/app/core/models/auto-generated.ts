//     This code was generated by a Reinforced.Typings tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.

export interface UiState
{
	fileStateState?: FileState;
	proxyState: ProxyState;
	settingsHolder: FluxzySettingsHolder;
}
export interface ProxyState
{
	isSystemProxyOn: boolean;
	isListening: boolean;
	boundConnections: ProxyEndPoint[];
}
export interface ProxyEndPoint
{
	address: string;
	port: number;
}
export interface ProxyBindPoint
{
	address: string;
	port: number;
	default: boolean;
}
export interface ArchivingPolicy
{
	type: number;
	directory: string;
	none: ArchivingPolicy;
}
export interface FileState
{
	identifier: string;
	workingDirectory: string;
	mappedFile?: string;
	changed: boolean;
	lastModification: Date;
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
	anticipatedConnectionPerHost: number;
	registerAsSystemProxy: boolean;
	throttleKBytePerSecond: number;
	serverProtocols: number;
	throttleIntervalCheck: any;
	caCertificate: any;
	certificateCacheDirectory: string;
	autoInstallCertificate: boolean;
	checkCertificateRevocation: boolean;
	disableCertificateCache: boolean;
	byPassHost: string[];
	maxHeaderLength: number;
	archivingPolicy: ArchivingPolicy;
	alterationRules: any[];
}
export interface ExchangeState
{
	exchanges: ExchangeInfo[];
	count: number;
	startIndex: number;
	endIndex: number;
	totalCount: number;
}
export interface ExchangeBrowsingState
{
	startIndex?: number;
	endIndex?: number;
	count: number;
}
export interface ExchangeInfo
{
	id: number;
	connectionId: number;
	httpVersion: string;
	requestHeader: RequestHeaderInfo;
	responseHeader: ResponseHeaderInfo;
	metrics: ExchangeMetrics;
	fullUrl: string;
	knownAuthority: string;
	method: string;
	path: string;
	contentType: string;
	done: boolean;
	statusCode: number;
	egressIp: string;
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
	localAddress: string;
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
	authority: AuthorityInfo;
	sslInfo: SslInfo;
	requestProcessed: number;
	dnsSolveStart: Date;
	dnsSolveEnd: Date;
	tcpConnectionOpening: Date;
	tcpConnectionOpened: Date;
	sslNegotiationStart: Date;
	sslNegotiationEnd: Date;
	localPort: number;
	localAddress: string;
	remoteAddress: string;
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
	remoteCertificateIssuer: string;
	remoteCertificateSubject: string;
	localCertificateSubject: string;
	localCertificateIssuer: string;
	negotiatedApplicationProtocol: string;
	keyExchangeAlgorithm: string;
	hashAlgorithm: number;
	cipherAlgorithm: number;
}
