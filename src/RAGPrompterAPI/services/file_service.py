import os
import asyncio
import re
import tempfile
from fastapi import UploadFile
from typing import List, Dict, Any

class FileService:
    def __init__(self):
        self.BASE_DIR = "projects"
        self.CHUNK_SIZE = 1024 * 8  # 8KB chunks
        os.makedirs(self.BASE_DIR, exist_ok=True)

    def _validate_project_name(self, project_name: str) -> str:
        """
        Validate and sanitize project name.
        """
        sanitized = re.sub(r'[^\w\-]', '', project_name)
        if not sanitized:
            raise ValueError("Invalid project name. Use only letters, numbers, dashes, and underscores.")
        return sanitized

    async def _save_file_streaming(self, file_path: str, file: UploadFile) -> tuple[int, str]:
        """
        Save a single file using streaming.

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

    def _is_archive_file(self, filename: str) -> bool:
        """
        Check if a filename ends with a supported archive extension.
        """
        lower = filename.lower()
        return (
            lower.endswith(".zip") or
            lower.endswith(".tar") or
            lower.endswith(".tar.gz") or
            lower.endswith(".tgz") or
            lower.endswith(".rar")
        )

    def _get_unique_filepath(self, dest_folder: str, filename: str) -> str:
        """
        Generate a unique file path in dest_folder by appending a counter if needed.
        """
        dest_path = os.path.join(dest_folder, filename)
        if not os.path.exists(dest_path):
            return dest_path

        name, ext = os.path.splitext(filename)
        counter = 1
        while True:
            new_filename = f"{name}_{counter}{ext}"
            new_dest_path = os.path.join(dest_folder, new_filename)
            if not os.path.exists(new_dest_path):
                return new_dest_path
            counter += 1

    def _extract_archive_recursive(self, archive_path: str, dest_folder: str,
                               extracted_files: List[str], errors: List[str],
                               original_filename: str = None) -> None:
        """
        Recursively extract supported archive files into dest_folder.
        Uses original_filename for the initial archive type check instead of the temporary file path.
        """
        # For the initial extraction, use the original filename to determine the type
        archive_to_check = original_filename if original_filename else archive_path
        lower_archive = archive_to_check.lower()

        try:
            if lower_archive.endswith(".zip"):
                import zipfile
                if not zipfile.is_zipfile(archive_path):
                    errors.append(f"File is not a valid zip archive.")
                    return
                with zipfile.ZipFile(archive_path, 'r') as z:
                    for member in z.infolist():
                        # Skip directories
                        if member.is_dir():
                            continue
                        # Use only the basename (thus flattening the structure)
                        filename = os.path.basename(member.filename)
                        if not filename:
                            continue
                        dest_path = self._get_unique_filepath(dest_folder, filename)
                        with z.open(member) as file_content, open(dest_path, 'wb') as f_out:
                            f_out.write(file_content.read())
                        extracted_files.append(os.path.basename(dest_path))
                        # Recursively process if the extracted file is an archive
                        if self._is_archive_file(dest_path):
                            self._extract_archive_recursive(dest_path, dest_folder, extracted_files, errors)
                            # Optionally remove the nested archive after extraction
                            os.remove(dest_path)

            elif lower_archive.endswith(".tar") or lower_archive.endswith(".tar.gz") or lower_archive.endswith(".tgz"):
                import tarfile
                try:
                    with tarfile.open(archive_path, 'r:*') as tar:
                        for member in tar.getmembers():
                            if member.isdir():
                                continue
                            file_obj = tar.extractfile(member)
                            if file_obj is None:
                                continue
                            filename = os.path.basename(member.name)
                            if not filename:
                                continue
                            dest_path = self._get_unique_filepath(dest_folder, filename)
                            with open(dest_path, 'wb') as f_out:
                                f_out.write(file_obj.read())
                            extracted_files.append(os.path.basename(dest_path))
                            if self._is_archive_file(dest_path):
                                self._extract_archive_recursive(dest_path, dest_folder, extracted_files, errors)
                                os.remove(dest_path)
                except tarfile.TarError as e:
                    errors.append(f"Error extracting tar archive {archive_path}: {str(e)}")

            elif lower_archive.endswith(".rar"):
                try:
                    import rarfile
                except ImportError:
                    errors.append("rarfile module not installed, cannot extract rar archives.")
                    return
                try:
                    with rarfile.RarFile(archive_path) as rf:
                        for member in rf.infolist():
                            if member.isdir():
                                continue
                            filename = os.path.basename(member.filename)
                            if not filename:
                                continue
                            dest_path = self._get_unique_filepath(dest_folder, filename)
                            with rf.open(member) as file_content, open(dest_path, 'wb') as f_out:
                                f_out.write(file_content.read())
                            extracted_files.append(os.path.basename(dest_path))
                            if self._is_archive_file(dest_path):
                                self._extract_archive_recursive(dest_path, dest_folder, extracted_files, errors)
                                os.remove(dest_path)
                except rarfile.Error as e:
                    errors.append(f"Error extracting rar archive {archive_path}: {str(e)}")
            else:
                errors.append(f"Unsupported archive format: {archive_to_check}")
        except Exception as e:
            errors.append(f"Error processing archive {archive_to_check}: {str(e)}")

    async def save_files(self, project_name: str, file: UploadFile) -> Dict[str, Any]:
        """
        Accept a single uploaded archive file, extract its contents recursively,
        and save all files (without preserving any folder hierarchy) into the project folder.
        
        The extraction supports common archive formats (e.g. .zip, .tar, .tar.gz, .tgz, .rar).
        If any of the extracted files are archives themselves, they are recursively extracted.
        """
        try:
            project_name = self._validate_project_name(project_name)
            project_dir = os.path.join(self.BASE_DIR, project_name)
            os.makedirs(project_dir, exist_ok=True)

            if not self._is_archive_file(file.filename):
                raise Exception("Uploaded file is not a supported archive format.")

            # Save the uploaded archive to a temporary file
            with tempfile.NamedTemporaryFile(delete=False) as tmp:
                temp_archive_path = tmp.name

            bytes_written, error = await self._save_file_streaming(temp_archive_path, file)
            if error:
                raise Exception("Failed to save the uploaded archive: " + error)

            extracted_files = []
            errors = []
            # Pass the original filename to the extraction function
            self._extract_archive_recursive(temp_archive_path, project_dir, extracted_files, errors, file.filename)

            # Remove the temporary archive file
            os.remove(temp_archive_path)

            return {
                "project": project_name,
                "extracted_files": extracted_files,
                "errors": errors
            }

        except Exception as e:
            raise Exception(f"Failed to save archive: {str(e)}")

    async def delete_project(self, project_name: str) -> bool:
        """
        Delete an entire project and all its files.
        """
        try:
            project_name = self._validate_project_name(project_name)
            project_dir = os.path.join(self.BASE_DIR, project_name)
            if not os.path.exists(project_dir):
                raise FileNotFoundError(f"Project {project_name} not found")
            import shutil
            shutil.rmtree(project_dir)
            return True
        except FileNotFoundError:
            raise
        except Exception as e:
            raise Exception(f"Failed to delete project: {str(e)}")

    async def get_project_files(self, project_name: str) -> List[str]:
        """
        Get a list of all files in a specific project directory.
        Returns an empty list if the project doesn't exist.
        """
        try:
            project_name = self._validate_project_name(project_name)
            project_dir = os.path.join(self.BASE_DIR, project_name)
            if not os.path.exists(project_dir):
                return []
            files = os.listdir(project_dir)
            return files
        except Exception:
            return []

    async def delete_file(self, project_name: str, filename: str) -> bool:
        """
        Delete a specific file from a project.
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
        Get a list of all projects.
        """
        try:
            projects = [d for d in os.listdir(self.BASE_DIR)
                        if os.path.isdir(os.path.join(self.BASE_DIR, d))]
            return projects
        except Exception as e:
            raise Exception(f"Failed to list projects: {str(e)}")