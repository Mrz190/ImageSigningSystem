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
      navigate("/auth");
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
    navigate("/auth", {replace: true});
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

  const handleForceUpload = async () => {
    if (!file) return;

    setLoading(true);
    try {
      const role = sessionStorage.getItem("role");
      const userPasswordHash = sessionStorage.getItem("userPasswordHash");
      const local_realm = sessionStorage.getItem("realm");
      const local_username = sessionStorage.getItem("username");

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
    } finally {
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
    navigate("/users", { replace: true });
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
        const serverMessage = await response.text();
        throw new Error(serverMessage || "Failed to change data");
      }
  
      alert("Data updated successfully!");
      setShowChangeDataModal(false);
    } catch (err) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };
  

  const handleChangeSupportEmail = async (event) => {
    event.preventDefault();

    if (!supportEmail) {
      alert("Please enter a new support email address.");
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(supportEmail)) {
      alert("Please enter a valid email address.");
      return;
    }

    setLoading(true);

    try {
      const userPasswordHash = localStorage.getItem("userPasswordHash");
      const local_realm = localStorage.getItem("realm");
      const local_username = localStorage.getItem("username");

      const local_HA1 = userPasswordHash;
      const uri = "/Admin/change-support-mail";

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

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: "POST",
        headers: {
          Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ supportEmail: supportEmail }),
      });

      if (!response.ok) {
        throw new Error("Failed to change support email");
      }

      alert("Support email updated successfully!");
      setShowChangeSupportEmailModal(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };


  const fetchUserData = async () => {
    setLoading(true);
    try {
      const userPasswordHash = sessionStorage.getItem("userPasswordHash");
      const local_realm = sessionStorage.getItem("realm");
      const local_username = sessionStorage.getItem("username");

      const local_HA1 = userPasswordHash;
      const uri = "/Account/get-data";

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

      const digest = calculateDigest(local_HA1, nonce, uri, "GET", qop, nc, cnonce);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: "GET",
        headers: {
          Authorization: `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`,
        },
      });

      if (!response.ok) {
        throw new Error("Failed to fetch user data");
      }

      const data = await response.json();
      setChangeData({ username: data.userName, email: data.email });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const openChangeDataModal = async () => {
    try {
      await fetchUserData();
      setShowChangeDataModal(true);
    } catch (error) {
      alert('Failed to load user data for modal');
    }
  };

  const openChangeSupportEmailModal = () => {
    setShowChangeSupportEmailModal(true);
  };

  return (
    <div>
      <div className="header-container">
        <div className="admin-btn-wrapper">
          <button className="edit-btn" onClick={openChangeDataModal}>
            Change Username/Email
          </button>
        </div>
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

        {showChangeSupportEmailModal && (
          <div className="modal">
            <div className="modal-content">
              <form onSubmit={handleChangeSupportEmail}>
                <label className="label-form" htmlFor="support-email">Support Email</label>
                <input
                  className="reglog-input edit-input"
                  type="email"
                  id="support-email"
                  value={supportEmail}
                  onChange={(e) => setSupportEmail(e.target.value)}
                />
                <button type="submit" className="send-btn" disabled={loading}>
                  {loading ? "Saving..." : "Submit"}
                </button>
                <br />
                <button type="button" className="send-btn" onClick={() => setShowChangeSupportEmailModal(false)}>
                  Close
                </button>
              </form>
            </div>
          </div>
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

      {showChangeDataModal && (
        <div className="modal">
          <div className="modal-content">
            <form onSubmit={handleChangeData}>
              <label className="label-form" htmlFor="username">Username</label>
              <input className="reglog-input edit-input"
                type="text"
                id="username"
                value={changeData.username}
                onChange={(e) => setChangeData({ ...changeData, username: e.target.value })}
              />
              <label className="label-form" htmlFor="email">Email</label>
              <input className="reglog-input edit-input"
                type="email"
                id="email"
                value={changeData.email}
                onChange={(e) => setChangeData({ ...changeData, email: e.target.value })}
              />
              <button type="submit" className="send-btn" disabled={loading}>
                {loading ? "Saving..." : "Submit"}
              </button>
              <br />
              <button type="button" className="send-btn" onClick={() => setShowChangeDataModal(false)}>
                Close
              </button>
            </form>
          </div>
        </div>
      )}

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
