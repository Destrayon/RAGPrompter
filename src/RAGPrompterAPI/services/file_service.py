import os
import asyncio
from fastapi import UploadFile
from typing import List, Dict, Any
import re

class FileService:
    def __init__(self):
        self.BASE_DIR = "projects"
        self.CHUNK_SIZE = 1024 * 8  # 8KB chunks
        os.makedirs(self.BASE_DIR, exist_ok=True)

    def _validate_project_name(self, project_name: str) -> str:
        """
        Validate and sanitize project name
        """
        sanitized = re.sub(r'[^\w\-]', '', project_name)
        if not sanitized:
            raise ValueError("Invalid project name. Use only letters, numbers, dashes, and underscores.")
        return sanitized

    async def _save_file_streaming(self, file_path: str, file: UploadFile) -> tuple[int, str]:
        """
        Save a single file using streaming
        
        Returns:
            tuple: (bytes_written, error_message)
        """
        try:
            bytes_written = 0
            with open(file_path, "wb") as f:
                while chunk := await file.read(self.CHUNK_SIZE):
                    f.write(chunk)
                    bytes_written += len(chunk)
            return bytes_written, ""
        except Exception as e:
            return 0, str(e)

    async def save_files(self, project_name: str, files: List[UploadFile]) -> Dict[str, Any]:
        """
        Save multiple uploaded files to the specific project directory using streaming
        """
        try:
            project_name = self._validate_project_name(project_name)
            project_dir = os.path.join(self.BASE_DIR, project_name)
            os.makedirs(project_dir, exist_ok=True)

            successful_files = []
            failed_files = []

            async def process_file(file: UploadFile):
                file_path = os.path.join(project_dir, file.filename)
                size, error = await self._save_file_streaming(file_path, file)
                
                if error:
                    failed_files.append({
                        "filename": file.filename,
                        "size": 0,
                        "status": "failed",
                        "message": error
                    })
                else:
                    successful_files.append({
                        "filename": file.filename,
                        "size": size,
                        "status": "success"
                    })

            # Create tasks for all files
            tasks = [process_file(file) for file in files]
            await asyncio.gather(*tasks)

            return {
                "project": project_name,
                "files": successful_files,
                "failed_files": failed_files
            }

        except Exception as e:
            raise Exception(f"Failed to save files: {str(e)}")

    async def delete_project(self, project_name: str) -> bool:
        """
        Delete an entire project and all its files
        
        Args:
            project_name (str): Name of the project to delete
            
        Returns:
            bool: True if project was deleted successfully
            
        Raises:
            FileNotFoundError: If project doesn't exist
            Exception: For other errors
        """
        try:
            project_name = self._validate_project_name(project_name)
            project_dir = os.path.join(self.BASE_DIR, project_name)
            
            if not os.path.exists(project_dir):
                raise FileNotFoundError(f"Project {project_name} not found")
                
            # Remove directory and all its contents
            import shutil
            shutil.rmtree(project_dir)
            return True
            
        except FileNotFoundError:
            raise
        except Exception as e:
            raise Exception(f"Failed to delete project: {str(e)}")

    async def get_project_files(self, project_name: str) -> List[str]:
        """
        Get list of all files in a specific project directory.
        Returns empty list if project doesn't exist.
        """
        try:
            project_name = self._validate_project_name(project_name)
            project_dir = os.path.join(self.BASE_DIR, project_name)
            if not os.path.exists(project_dir):
                return []
            files = os.listdir(project_dir)
            return files
        except Exception as e:
            # Return empty list for any errors
            return []

    async def delete_file(self, project_name: str, filename: str) -> bool:
        """
        Delete a specific file from a project
        
        Args:
            project_name (str): Name of the project
            filename (str): Name of the file to delete
            
        Returns:
            bool: True if file was deleted successfully
            
        Raises:
            FileNotFoundError: If file or project doesn't exist
            Exception: For other errors
        """
        try:
            project_name = self._validate_project_name(project_name)
            project_dir = os.path.join(self.BASE_DIR, project_name)
            file_path = os.path.join(project_dir, filename)
            
            if not os.path.exists(project_dir):
                raise FileNotFoundError(f"Project {project_name} not found")
                
            if not os.path.exists(file_path):
                raise FileNotFoundError(f"File {filename} not found in project {project_name}")
                
            os.remove(file_path)
            return True
            
        except FileNotFoundError:
            raise
        except Exception as e:
            raise Exception(f"Failed to delete file: {str(e)}")

    async def get_projects(self) -> List[str]:
        """
        Get list of all projects
        """
        try:
            projects = [d for d in os.listdir(self.BASE_DIR) 
                       if os.path.isdir(os.path.join(self.BASE_DIR, d))]
            return projects
        except Exception as e:
            raise Exception(f"Failed to list projects: {str(e)}")