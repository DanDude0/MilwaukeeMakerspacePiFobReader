using System;
using System.IO;
using Newtonsoft.Json;
using SQLite;

namespace MmsPiFobReader
{
	class LocalController : IController
	{
		public const string FileName = "snapshot.sqlite3";

		private ReaderStatus status;
		private SQLiteConnection db;

		public LocalController(ReaderStatus statusIn)
		{
			status = statusIn;

			// SSDP Not working? Use override file
			if (File.Exists(FileName))
				db = new SQLiteConnection(FileName, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex, false);
			else
				throw new Exception("No local snapshot found");

			status.Controller = "Local";
			status.Warning = "No\nServer!";
		}

		public void Dispose()
		{
			db?.Close();
			db?.Dispose();
			db = null;
		}

		public ReaderResult Initialize()
		{
			var result = db.FindWithQuery<ReaderResult>(
				@"SELECT 
					r.name AS 'Name',
					r.timeout AS 'Timeout',
					r.enabled AS 'Enabled',
					g.name AS 'Group',
					r.settings AS 'Settings'
				FROM
					reader r
					INNER JOIN `group` g
						ON r.group_id = g.group_id
				WHERE
					r.reader_id = ?
				LIMIT 1",
				status.Id);

			if (result == null)
				throw new Exception("Could not find reader id");

			return result;
		}

		public AuthenticationResult Authenticate(string key)
		{
			if (key.StartsWith("W26#"))
				key = key.Substring(6);

			var result = db.FindWithQuery<AuthenticationResult>(@"
				SELECT 
					m.member_id AS 'Id',
					m.name AS 'Name',
					m.type AS 'Type',
					m.apricot_admin AS Admin,
					m.joined AS Joined,
					m.expires AS Expiration,
					date(m.expires, '+7 day') > date('now') AS AccessGranted
				FROM
					member m
					INNER JOIN keycode k
						ON m.member_id = k.member_id 
				WHERE
					k.keycode_id = ?
				LIMIT 1;",
				key);

			db.Execute(@"
				INSERT INTO
					attempt (
						reader_id,
						keycode,
						member_id,
						access_granted,
						login,
						logout,
						action,
						attempt_time
					)
				VALUES	(
					?,
					?,
					?,
					?,
					1,
					0,
					'Login',
					date('now')
				);",
				status.Id,
				key,
				result?.Id ?? -1,
				result?.AccessGranted ?? false);

			if (result == null)
				throw new Exception("Invalid key");

			return result;
		}

		public void Logout(string key)
		{
			var result = db.FindWithQuery<AuthenticationResult>(@"
				SELECT 
					m.member_id AS 'Id',
					m.name AS 'Name',
					m.type AS 'Type',
					m.apricot_admin AS Admin,
					m.joined AS Joined,
					m.expires AS Expiration,
					date(m.expires, '+7 day') > date('now') AS AccessGranted
				FROM
					member m
					INNER JOIN keycode k
						ON m.member_id = k.member_id 
				WHERE
					k.keycode_id = ?
				LIMIT 1;",
				key);

			db.Execute(@"
				INSERT INTO
					attempt (
						reader_id,
						keycode,
						member_id,
						access_granted,
						login,
						logout,
						action,
						attempt_time
					)
				VALUES	(
					?,
					?,
					?,
					?,
					0,
					1,
					'Logout',
					date('now')
				);",
				status.Id,
				key,
				result?.Id ?? -1,
				result?.AccessGranted ?? false);
		}

		public void Action(string key, string details)
		{
			var result = db.FindWithQuery<AuthenticationResult>(@"
				SELECT 
					m.member_id AS 'Id',
					m.name AS 'Name',
					m.type AS 'Type',
					m.apricot_admin AS Admin,
					m.joined AS Joined,
					m.expires AS Expiration,
					date(m.expires, '+7 day') > date('now') AS AccessGranted
				FROM
					member m
					INNER JOIN keycode k
						ON m.member_id = k.member_id 
				WHERE
					k.keycode_id = ?
				LIMIT 1;",
				key);

			db.Execute(@"
				INSERT INTO
					attempt (
						reader_id,
						keycode,
						member_id,
						access_granted,
						login,
						logout,
						action,
						attempt_time
					)
				VALUES	(
					?,
					?,
					?,
					?,
					0,
					0,
					?,
					date('now')
				);",
				status.Id,
				key,
				result?.Id ?? -1,
				result?.AccessGranted ?? false,
				details);
		}

		public void Charge(string key, string details, string description, decimal amount)
		{
			throw new NotImplementedException();
		}
	}
}
