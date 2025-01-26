from fastapi import FastAPI
import uvicorn
from routers import files

# Initialize FastAPI app
app = FastAPI()

# Basic routes
@app.get("/")
def read_root():
    return {"status": "ok"}

app = FastAPI(title="Project-based File Upload Service")

app.include_router(files.router, prefix="/files", tags=["files"])

# Run the server
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)