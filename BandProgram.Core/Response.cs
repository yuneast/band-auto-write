using System;

namespace BandProgram
{
	public class Response
	{
		private bool success;

		private string message;

		public Response()
		{
			this.success = false;
			this.message = "";
		}

		public Response(bool success, string message)
		{
			this.success = success;
			this.message = message;
		}

		public string getMessage()
		{
			return this.message;
		}

		public bool getSuccess()
		{
			return this.success;
		}

		public void setMessage(string message)
		{
			this.message = message;
		}

		public void setSuccess(bool success)
		{
			this.success = success;
		}
	}
}