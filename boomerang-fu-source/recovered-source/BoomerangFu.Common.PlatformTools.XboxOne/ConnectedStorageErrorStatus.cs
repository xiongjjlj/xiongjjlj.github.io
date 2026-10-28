namespace BoomerangFu.Common.PlatformTools.XboxOne
{
	public enum ConnectedStorageErrorStatus : uint
	{
		Success = 0u,
		BlobNotFound = 2156068872u,
		ContainerNotInSync = 2156068874u,
		ContainerSyncFailed = 2156068875u,
		InvalidContainerName = 2156068865u,
		NoAccess = 2156068866u,
		NoXboxLiveInfo = 2156068873u,
		OutOfLocalStorage = 2156068867u,
		ProvidedBufferTooSmall = 2156068871u,
		QuotaExceeded = 2156068870u,
		UpdateTooBig = 2156068869u,
		UserCanceled = 2156068868u,
		Error = 268435455u
	}
}
