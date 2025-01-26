from fastapi import APIRouter, UploadFile, File, HTTPException, Path
from typing import List, Dict, Any
from services.file_service import FileService

router = APIRouter()
file_service = FileService()

@router.post("/{project_name}/upload")
async def upload_files(
    project_name: str = Path(..., description="Name of the project"),
    files: List[UploadFile] = File(...),
):
    """
    Upload multiple files to a specific project directory using streaming
    """
    try:
        result = await file_service.save_files(project_name, files)
        return result
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@router.get("/{project_name}/list")
async def list_project_files(
    project_name: str = Path(..., description="Name of the project")
):
    """
    List all files stored in a specific project
    """
    try:
        files = await file_service.get_project_files(project_name)
        return files
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.delete("/{project_name}")
async def delete_project(
    project_name: str = Path(..., description="Name of the project to delete")
):
    """
    Delete an entire project and all its files
    """
    try:
        result = await file_service.delete_project(project_name)
        return {"message": f"Project {project_name} deleted successfully"}
    except FileNotFoundError:
        raise HTTPException(status_code=404, detail=f"Project {project_name} not found")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.delete("/{project_name}/{filename}")
async def delete_file(
    project_name: str = Path(..., description="Name of the project"),
    filename: str = Path(..., description="Name of the file to delete")
):
    """
    Delete a specific file from a project
    """
    try:
        result = await file_service.delete_file(project_name, filename)
        return {"message": f"File {filename} deleted successfully"}
    except FileNotFoundError:
        raise HTTPException(status_code=404, detail=f"File {filename} not found in project {project_name}")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/projects")
async def list_projects():
    """
    List all available projects
    """
    try:
        projects = await file_service.get_projects()
        return projects
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))