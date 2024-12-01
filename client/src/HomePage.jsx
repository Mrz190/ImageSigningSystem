import React, { useState, useEffect } from 'react';
import { useNavigate } from "react-router-dom";
import Images from './Images';
import CryptoJS from 'crypto-js';
import config from './config';
import "./styles/home-styles.css";

const HomePage = () => {
  const [file, setFile] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [refreshImages, setRefreshImages] = useState(false);
  const [isUserRole, setIsUserRole] = useState(false);
  const [isAdminRole, setIsAdminRole] = useState(false);
  const [showResumeModal, setShowResumeModal] = useState(false); // Для модалки с подтверждением
  const [isForceUpload, setIsForceUpload] = useState(false); // Для выполнения force-upload
  const navigate = useNavigate();

  useEffect(() => {
    const role = localStorage.getItem("role");
    setIsUserRole(role === "User");
    setIsAdminRole(role === "Admin");
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("userPasswordHash");
    localStorage.removeItem("token");
    localStorage.removeItem("realm");
    localStorage.removeItem("username");
    localStorage.removeItem("role");
    window.location.href = "/auth";
  };

  const handleFileChange = (event) => {
    const selectedFile = event.target.files[0];
    if (selectedFile) {
      setFile(selectedFile);
    }
  };

  const handleRedirectToRootPage = () => {
    navigate("/", {replace: true});
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!file) {
      alert("Please select a file.");
      return;
    }

    setLoading(true);
    try {
      const role = localStorage.getItem("role");
      const userPasswordHash = localStorage.getItem("userPasswordHash");
      const local_realm = localStorage.getItem("realm");
      const local_username = localStorage.getItem("username");

      if (!role || !userPasswordHash || !local_username) {
        throw new Error("User is not authenticated or role is missing");
      }

      const local_HA1 = userPasswordHash;
      const uri = "/User/upload";

      const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
        method: "GET",
      });

      if (!nonceResponse.ok) {
        throw new Error("Failed to fetch nonce");
      }

      const nonceData = await nonceResponse.json();
      const nonce = nonceData.nonce;

      const qop = "auth";
      const nc = "00000001";
      const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);

      const digest = calculateDigest(local_HA1, nonce, uri, "POST", qop, nc, cnonce);

      const formData = new FormData();
      formData.append("file", file);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: "POST",
        headers: {
          Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
        },
        body: formData,
      });

      if (response.status === 202) {
        const message = await response.text();
        if (message.includes("In your image we found signature")) {
          setShowResumeModal(true);
        }
      } else if (!response.ok) {
        throw new Error("Failed to upload image");
      } else {
        alert("Image uploaded successfully!");
        setFile(null);
        setRefreshImages(prevState => !prevState);
      }

      setLoading(false);
    } catch (err) {
      setError(err.message);
      setLoading(false);
    }
  };

  const handleForceUpload = async () => {
    if (!file) return;

    setLoading(true);
    try {
      const role = localStorage.getItem("role");
      const userPasswordHash = localStorage.getItem("userPasswordHash");
      const local_realm = localStorage.getItem("realm");
      const local_username = localStorage.getItem("username");

      const local_HA1 = userPasswordHash;
      const uri = "/User/force-upload";

      const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
        method: "GET",
      });

      if (!nonceResponse.ok) {
        throw new Error("Failed to fetch nonce");
      }

      const nonceData = await nonceResponse.json();
      const nonce = nonceData.nonce;

      const qop = "auth";
      const nc = "00000001";
      const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);

      const digest = calculateDigest(local_HA1, nonce, uri, "POST", qop, nc, cnonce);

      const formData = new FormData();
      formData.append("file", file);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: "POST",
        headers: {
          Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
        },
        body: formData,
      });

      if (!response.ok) {
        throw new Error("Failed to force upload image");
      }
      setShowResumeModal(false);
      setFile(null);
      setRefreshImages(prevState => !prevState);
      alert("Image uploaded successfully!");
    } catch (err) {
      setError(err.message);
      setLoading(false);
    }finally{
      setLoading(false);
    }
  };

  const calculateDigest = (_local_HA1, nonce, uri, method, qop, nc, cnonce) => {
    const HA1 = _local_HA1;
    const HA2 = CryptoJS.MD5(method + ":" + uri).toString(CryptoJS.enc.Hex);
    const response = CryptoJS.MD5(HA1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + HA2).toString(CryptoJS.enc.Hex);
    return response;
  };

  const handleNavigateToUsers = () => {
    navigate("/users");
  };

  return (
    <div>
      <button className="logout-btn" onClick={handleLogout}>
        Logout &#8625;
      </button>
      <div className="global-container">
        {isUserRole && (
          <div className="upload-container">
            <form onSubmit={handleSubmit} method="POST" encType="multipart/form-data">
              <div className="fileform-container">
                <input
                  type="file"
                  id="actual-btn"
                  hidden
                  accept=".png"
                  onChange={handleFileChange}
                />
                <label className="file-label" htmlFor="actual-btn">
                  Choose File
                </label>
                <span className="chosen-image" id="file-chosen">
                  {file ? file.name : "No file chosen"}
                </span>
                <button type="submit" className="upload-btn" disabled={loading}>
                  {loading ? "Uploading..." : "Upload image"}
                </button>
              </div>
            </form>
            <button className="redirect-btn" onClick={handleRedirectToRootPage}>Check signature</button>
          </div>
        )}

        {isAdminRole && (
          <button className="redirect-btn back-btn" onClick={handleNavigateToUsers}>
            Manage Users
          </button>
        )}

        <div className="images-container">
          {loading ? (
            <div className="loading-container">
              <div className="spinner-container">
                <div className="spinner">
                  <div></div>
                  <div></div>
                  <div></div>
                  <div></div>
                  <div></div>
                  <div></div>
                  <div></div>
                  <div></div>
                  <div></div>
                  <div></div>
                </div>
                <p className="loading">Loading...</p>
              </div>
            </div>
          ) : (
            <Images refreshImages={refreshImages} />
          )}
        </div>
      </div>

      {error && <div className="error">{error}</div>}

      {/* Модалка для подтверждения загрузки */}
      {showResumeModal && (
        <div className="modal">
          <div className="modal-content">
            <p>In your image we found signature. Do you want to resume signing image (previous signature will be deleted)?</p>
            <button className="choose-btn" onClick={handleForceUpload}>Yes</button>
            <button className="choose-btn" onClick={() => setShowResumeModal(false)}>No</button>
          </div>
        </div>
      )}
    </div>
  );
};

export default HomePage;
