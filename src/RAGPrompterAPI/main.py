from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import uvicorn
from routers import files

# Initialize FastAPI app once with title
app = FastAPI(title="Project-based File Upload Service")

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"]
)

# Basic routes
@app.get("/")
def read_root():
    return {"status": "ok"}

# Include routers
app.include_router(files.router, prefix="/files", tags=["files"])

# Run the server
if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)