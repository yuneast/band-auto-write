using System;
using System.Collections.Generic;

namespace BandProgram
{
	public class Post
	{
		public int idx;

		public string contents;
		public List<ImageFile> images;

        public string comment_contents = "";
        public List<ImageFile> comment_images;

        public bool has_comment = false;
        public Post()
        {
		}
	}
}