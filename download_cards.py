from pymongo import MongoClient
import json
from datetime import datetime

# MongoDB connection details
connection_uri = "mongodb+srv://admin:test123@cluster0.j0hbc7q.mongodb.net/Empire-Deckbuilder?retryWrites=true&w=majority&appName=Cluster0"
database_name = "Empire-Deckbuilder"
collection_name = "CardsForGame"

def serialize_document(doc):
    """Convert MongoDB document to JSON-serializable format"""
    if "_id" in doc:
        doc["_id"] = str(doc["_id"])
    
    # Handle any datetime objects
    for key, value in doc.items():
        if isinstance(value, datetime):
            doc[key] = value.isoformat()
    
    return doc

def main():
    try:
        print("Connecting to MongoDB...")
        client = MongoClient(connection_uri)
        
        # Access database and collection
        db = client[database_name]
        collection = db[collection_name]
        
        print(f"Connected to database: {database_name}")
        print(f"Accessing collection: {collection_name}")
        
        # Get count first
        count = collection.count_documents({})
        print(f"Found {count} cards in the collection")
        
        if count == 0:
            print("No cards found in the collection!")
            return
        
        # Fetch all documents
        print("Downloading all cards...")
        documents = list(collection.find({}))
        
        # Serialize documents
        serialized_docs = [serialize_document(doc.copy()) for doc in documents]
        
        # Save to JSON file
        output_file = "empire_cards.json"
        with open(output_file, "w", encoding="utf-8") as f:
            json.dump(serialized_docs, f, indent=2, ensure_ascii=False)
        
        print(f"Successfully exported {len(serialized_docs)} cards to {output_file}")
        
        # Show a sample card structure
        if serialized_docs:
            print("\nSample card structure:")
            print(json.dumps(serialized_docs[0], indent=2))
        
        # Show some basic stats
        print(f"\nBasic stats:")
        print(f"Total cards: {len(serialized_docs)}")
        
        # Try to identify card types if the field exists
        card_types = set()
        card_ids = []
        for card in serialized_docs:
            if "type" in card:
                card_types.add(card["type"])
            if "cardId" in card:
                card_ids.append(card["cardId"])
            elif "id" in card:
                card_ids.append(card["id"])
        
        if card_types:
            print(f"Card types found: {list(card_types)}")
        
        if card_ids:
            print(f"Card ID range: {min(card_ids)} - {max(card_ids)}")
        
    except Exception as e:
        print(f"Error: {e}")
    finally:
        if 'client' in locals():
            client.close()

if __name__ == "__main__":
    main()
