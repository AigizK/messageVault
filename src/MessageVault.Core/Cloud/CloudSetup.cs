using System;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace MessageVault.Cloud {

	public static class CloudSetup {

		public static IRetryPolicy RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(0.5), 3);

		public const string CheckpointMetadataName = "position";

		public static MessageWriter CreateAndInitWriter(CloudBlobContainer container) {
			
			container.CreateIfNotExists();
			var dataBlob = container.GetPageBlobReference(Constants.StreamFileName);
			var posBlob = container.GetPageBlobReference(Constants.PositionFileName);
			var pageWriter = new CloudPageWriter(dataBlob);
			var posWriter = new CloudCheckpointWriter(posBlob);
			var writer = new MessageWriter(pageWriter, posWriter);
			writer.Init();

			return writer;
		}

		public static string GetReadAccessSignature(CloudBlobContainer container) {
			
			var signature = container.GetSharedAccessSignature(new SharedAccessBlobPolicy {
				Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read, 
				// since Microsoft servers don't have an uptime longer than a year
				SharedAccessExpiryTime = DateTimeOffset.Now.AddYears(7),
			});
			return container.Uri + signature;
		}

		public static Tuple<CloudCheckpointReader, CloudPageReader> GetReaderRaw(string sas) {
			var uri = new Uri(sas);
			var container = new CloudBlobContainer(uri);

			var posBlob = container.GetPageBlobReference(Constants.PositionFileName);
			var dataBlob = container.GetPageBlobReference(Constants.StreamFileName);
			var position = new CloudCheckpointReader(posBlob);
			var messages = new CloudPageReader(dataBlob);
			return Tuple.Create(position, messages);
		}

		public static MessageReader GetReader(string sas) {
			var raw = GetReaderRaw(sas);
			return new MessageReader(raw.Item1, raw.Item2);
		}
	}

}