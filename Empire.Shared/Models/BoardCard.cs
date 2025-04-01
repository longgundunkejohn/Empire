using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Empire.Shared.Models
{
    public class BoardCard
    {
        public int CardId { get; set; }
        public bool IsExerted { get; set; } = false;
        public int Damage { get; set; } = 0;

        public BoardCard(int cardId)
        {
            CardId = cardId;
        }

        public void Rotate()
        {
            IsExerted = !IsExerted;
        }
    }
}