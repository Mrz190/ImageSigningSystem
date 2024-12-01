import React, { useState, useRef } from "react";
import { useNavigate } from "react-router-dom";
import config from './config';

const IndexPage = () => {
  const [image, setImage] = useState(null);
  const [fileName, setFileName] = useState(null);
  const [isImageUploaded, setIsImageUploaded] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);
  const fileInputRef = useRef(null);
  const uploadWrapRef = useRef(null);
  const navigate = useNavigate();

  const readURL = (input) => {
    if (input.files && input.files[0]) {
      const file = input.files[0];

      if (file.type !== "image/png") {
        setError("Please upload a PNG image.");
        return;
      } else {
        setError(null);
      }

      setFileName(file.name);
      const reader = new FileReader();
      reader.onload = (e) => {
        setImage(e.target.result);
        setIsImageUploaded(true);
      };
      reader.readAsDataURL(file);
    } else {
      removeUpload();
    }
  };

  const removeUpload = () => {
    setImage(null);
    setFileName(null);
    setIsImageUploaded(false);
    setError(null);
  };

  const handleFileSelect = () => {
    fileInputRef.current.click();
  };

  const handleSubmit = async (e, action) => {
    e.preventDefault();

    if (!image) {
      alert("Please upload a file before submitting.");
      return;
    }

    setIsSubmitting(true);

    const uri = action === "verify" 
      ? "/Unauthorized/verify-file-signature"
      : "/Unauthorized/find-signature";

    try {
      const formData = new FormData();
      formData.append("file", fileInputRef.current.files[0]);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: "POST",
        body: formData,
      });

      if (response.ok) {
        if (action === "verify") {
          alert("Signature valid!");
        } else {
          alert("Signature found!");
        }
      } else {
        if (action === "verify") {
          alert("Signature invalid!");
        } else {
          alert("No signature found!");
        }
      }
    } catch (error) {
      console.error("Error uploading file:", error);
      alert("There was an error uploading the file.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    uploadWrapRef.current.classList.add("image-dropping");
  };

  const handleDragLeave = () => {
    uploadWrapRef.current.classList.remove("image-dropping");
  };

  const handleDrop = (e) => {
    e.preventDefault();
    uploadWrapRef.current.classList.remove("image-dropping");
    const file = e.dataTransfer.files[0];
  
    if (file && file.type === "image/png") {
      // Manually add the file to the file input
      const dataTransfer = new DataTransfer();
      dataTransfer.items.add(file);
      fileInputRef.current.files = dataTransfer.files;
  
      setFileName(file.name);
      const reader = new FileReader();
      reader.onload = (e) => {
        setImage(e.target.result);
        setIsImageUploaded(true);
      };
      reader.readAsDataURL(file);
    } else {
      setError("Please upload a PNG image.");
    }
  };

  const getTruncatedFileName = (name) => {
    if (name && name.length > 25) {
      return name.slice(0, 25) + "...";
    }
    return name;
  };

  const goToHome = () => {
    navigate("/home");
  };

  const goToAuth = () => {
    navigate("/auth");
  };

  const isUserAuthenticated = () => {
    return localStorage.getItem("realm") && localStorage.getItem("username") && localStorage.getItem("role");
  };

  return (
    <div className="global-container index-container">
      {isUserAuthenticated() ? (
        <button className="go-home-auth-btn" onClick={goToHome}>
          To home page &#8629;
        </button>
      ) :
      (
        <button className="go-home-auth-btn" onClick={goToAuth}>
          Authorize now &#8629;
        </button>
      )}
      <form onSubmit={handleSubmit} className="file-upload-form">
        <div className="file-upload">
          <button
            type="button"
            className="file-upload-btn"
            onClick={handleFileSelect}
            disabled={isSubmitting}
          >
            Add Image
          </button>

          <div
            ref={uploadWrapRef}
            className="image-upload-wrap"
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
          >
            <input
              ref={fileInputRef}
              className="file-upload-input"
              type="file"
              onChange={(e) => readURL(e.target)}
              accept="image/png"
              style={{ display: "none" }}
            />
            <div className="drag-text">
              <h3>{isImageUploaded ? getTruncatedFileName(fileName) : "Drag and drop a PNG file or select Add Image"}</h3>
            </div>
          </div>

          {isImageUploaded && (
            <div className="file-upload-content">
              <img className="file-upload-image" src={image} alt="your image" />
              <div className="image-title-wrap">
                <button
                  type="button"
                  onClick={removeUpload}
                  className="remove-image"
                >
                  Remove <span className="image-title">Uploaded Image</span>
                </button>
              </div>
            </div>
          )}

          {error && <div className="error">{error}</div>}

          <button
            type="submit"
            className="submit-btn"
            disabled={isSubmitting || !image}
            onClick={(e) => handleSubmit(e, "verify")}
          >
            {isSubmitting ? "On review..." : "Verify signature"}
          </button>
          <br/>
          <button
            type="submit"
            className="submit-btn"
            disabled={isSubmitting || !image}
            onClick={(e) => handleSubmit(e, "find")}
          >
            {isSubmitting ? "On review..." : "Find signature"}
          </button>
        </div>
      </form>
    </div>
  );
};

export default IndexPage;
