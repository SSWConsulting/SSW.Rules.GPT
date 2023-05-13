using OpenAI.GPT3.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class TrimResult
    {
        public bool InputTooLong { get; set; }

        public int RemainingTokens { get; set; }

        public List<ChatMessage> Messages { get; set; } = null!;
    }
}
