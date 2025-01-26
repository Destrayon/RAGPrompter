from fastapi import FastAPI
import uvicorn

# Initialize FastAPI app
app = FastAPI()

# Basic routes
@app.get("/")
def read_root():
    return {"status": "ok"}

# Run the server
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)