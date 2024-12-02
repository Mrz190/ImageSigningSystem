import React, { useState, useEffect } from 'react';
import { useNavigate } from "react-router-dom";
import CryptoJS from 'crypto-js';
import Images from './Images';
import config from './config';
import "./styles/home-styles.css";

const HomePage = () => {
  const [file, setFile] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [refreshImages, setRefreshImages] = useState(false);
  const [isUserRole, setIsUserRole] = useState(false);
  const [isAdminRole, setIsAdminRole] = useState(false);
  const [showResumeModal, setShowResumeModal] = useState(false);
  const [showChangeDataModal, setShowChangeDataModal] = useState(false);
  const [changeData, setChangeData] = useState({ username: '', email: '' });
  const [isForceUpload, setIsForceUpload] = useState(false);
  const navigate = useNavigate();
  const [showChangeSupportEmailModal, setShowChangeSupportEmailModal] = useState(false);
  const [supportEmail, setSupportEmail] = useState('');

  useEffect(() => {
    const role = sessionStorage.getItem("role");
    if (!role) {
      navigate("/auth"); // Redirect to auth page if no role is found
    }
    setIsUserRole(role === "User");
    setIsAdminRole(role === "Admin");
  }, [navigate]);

  const handleLogout = () => {
    sessionStorage.removeItem("userPasswordHash");
    sessionStorage.removeItem("token");
    sessionStorage.removeItem("realm");
    sessionStorage.removeItem("username");
    sessionStorage.removeItem("role");
    navigate("/auth");
  };

  const handleFileChange = (event) => {
    const selectedFile = event.target.files[0];
    if (selectedFile) {
      setFile(selectedFile);
    }
  };

  const handleRedirectToRootPage = () => {
    navigate("/", { replace: true });
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!file) {
      alert("Please select a file.");
      return;
    }

    setLoading(true);
    try {
      const role = sessionStorage.getItem("role");
      const userPasswordHash = sessionStorage.getItem("userPasswordHash");
      const local_realm = sessionStorage.getItem("realm");
      const local_username = sessionStorage.getItem("username");

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

  const calculateDigest = (_local_HA1, nonce, uri, method, qop, nc, cnonce) => {
    const HA1 = _local_HA1;
    const HA2 = CryptoJS.MD5(method + ":" + uri).toString(CryptoJS.enc.Hex);
    const response = CryptoJS.MD5(HA1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + HA2).toString(CryptoJS.enc.Hex);
    return response;
  };

  const handleNavigateToUsers = () => {
    navigate("/users");
  };

  const handleChangeData = async (event) => {
    event.preventDefault();

    const { username, email } = changeData;

    if (!username || !email) {
      alert("Please fill in both username and email.");
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      alert("Please enter a valid email address.");
      return;
    }

    setLoading(true);

    try {
      const userPasswordHash = sessionStorage.getItem("userPasswordHash");
      const local_realm = sessionStorage.getItem("realm");
      const local_username = sessionStorage.getItem("username");

      const local_HA1 = userPasswordHash;
      const uri = "/Account/change-data";

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

      const digest = calculateDigest(local_HA1, nonce, uri, "PUT", qop, nc, cnonce);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: "PUT",
        headers: {
          Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ username, email }),
      });

      if (!response.ok) {
        throw new Error("Failed to change data");
      }

      alert("Data updated successfully!");
      setShowChangeDataModal(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <div className="header-container">
        {isAdminRole && (
          <div className="admin-btn-wrapper">
            <button className="edit-btn edit-support-btn" onClick={openChangeSupportEmailModal}>
              Change Support Email
            </button>
            <button className="edit-btn" onClick={openChangeDataModal}>
              Change Username/Email
            </button>
          </div>
        )}
        <button className="logout-btn" onClick={handleLogout}>
          Logout &#8625;
        </button>
      </div>
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
    </div>
  );
};

export default HomePage;
