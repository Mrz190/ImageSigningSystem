import React, { useState, useEffect } from 'react';
import CryptoJS from 'crypto-js';
import config from './config';

const Images = () => {
  const [images, setImages] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const role = localStorage.getItem('role');
  
  const fetchImages = async () => {
    if (loading) return;
    setLoading(true);

    try {
      const userPasswordHash = localStorage.getItem('userPasswordHash');
      const local_realm = localStorage.getItem('realm');
      const local_username = localStorage.getItem('username');

      if (!role || !userPasswordHash) {
        throw new Error('User is not authenticated or role is missing');
      }

      const local_HA1 = userPasswordHash;
      const uri = getUriForRole(role);

      const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
        method: 'GET',
      });

      if (!nonceResponse.ok) {
        throw new Error('Failed to fetch nonce');
      }

      const nonceData = await nonceResponse.json();
      const nonce = nonceData.nonce;

      const qop = 'auth';
      const nc = '00000001';
      const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);

      const digest = calculateDigest(local_HA1, nonce, uri, 'GET', qop, nc, cnonce);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`
        }
      });

      if (!response.ok) {
        throw new Error('Failed to fetch images');
      }

      const data = await response.json();
      setImages(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchImages();
  }, []);

  const calculateDigest = (_local_HA1, nonce, uri, method, qop, nc, cnonce) => {
    const HA1 = _local_HA1;
    const HA2 = CryptoJS.MD5(method + ":" + uri).toString(CryptoJS.enc.Hex);
    const response = CryptoJS.MD5(HA1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + HA2).toString(CryptoJS.enc.Hex);
    return response;
  };

  const getUriForRole = (role) => {
    switch (role) {
      case 'User':
        return '/User/get-user-images';
      case 'Support':
        return '/Support/get-support-images';
      case 'Admin':
        return '/Administrator/get-admin-images';
      default:
        throw new Error('Invalid role');
    }
  };

  const handleDownloadImage = async (id) => {
    try {
      setLoading(true);

      const userPasswordHash = localStorage.getItem('userPasswordHash');
      const local_realm = localStorage.getItem('realm');
      const local_username = localStorage.getItem('username');

      if (!role || !userPasswordHash) {
        throw new Error('User is not authenticated or role is missing');
      }

      const local_HA1 = userPasswordHash;
      const uri = `/User/download/${id}`;

      const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
        method: 'GET',
      });

      if (!nonceResponse.ok) {
        throw new Error('Failed to fetch nonce');
      }

      const nonceData = await nonceResponse.json();
      const nonce = nonceData.nonce;

      const qop = 'auth';
      const nc = '00000001';
      const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);

      const digest = calculateDigest(local_HA1, nonce, uri, 'GET', qop, nc, cnonce);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'GET',
        headers: {
          'Authorization': `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`
        },
      });

      if (!response.ok) {
        throw new Error('Failed to download image');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `image-${id}.png`;
      link.click();
      window.URL.revokeObjectURL(url);

      fetchImages();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteImage = async (id) => {
    if (!window.confirm('Are you sure you want to delete this image?')) return;

    try {
      setLoading(true);

      const userPasswordHash = localStorage.getItem('userPasswordHash');
      const local_realm = localStorage.getItem('realm');
      const local_username = localStorage.getItem('username');

      if (!role || !userPasswordHash) {
        throw new Error('User is not authenticated or role is missing');
      }

      const local_HA1 = userPasswordHash;
      const uri = `/User/delete-image/${id}`;

      const nonceResponse = await fetch(`${config.apiBaseUrl}/Account/LoginNonce`, {
        method: 'GET',
      });

      if (!nonceResponse.ok) {
        throw new Error('Failed to fetch nonce');
      }

      const nonceData = await nonceResponse.json();
      const nonce = nonceData.nonce;

      const qop = 'auth';
      const nc = '00000001';
      const cnonce = CryptoJS.lib.WordArray.random(4).toString(CryptoJS.enc.Hex);

      const digest = calculateDigest(local_HA1, nonce, uri, 'DELETE', qop, nc, cnonce);

      const response = await fetch(`${config.apiBaseUrl}${uri}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Digest username="${local_username}", realm="${local_realm}", nonce="${nonce}", uri="${uri}", algorithm="MD5", qop=${qop}, nc=${nc}, cnonce="${cnonce}", response="${digest}"`
        },
      });

      if (!response.ok) {
        throw new Error('Failed to delete image');
      }

      setImages(images.filter((image) => image.id !== id));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Loading...</div>;
  }

  if (error) {
    return <div>Error: {error}</div>;
  }

  if (role == "Admin") {
    return (
      <div className="images-container">
        <table className="images-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Status</th>
              <th>Support</th>
              <th>Reject</th>
              <th>View</th>
            </tr>
          </thead>
          <tbody>
            {images.map((image) => (
              <tr key={image.id}>
                <td>
                  {image.imageName && image.imageName.length > 15
                    ? `${image.imageName.substring(0, 15)}...`
                    : image.imageName || "No Name"}
                </td>
                <td>{image.status}</td>
                <td className="download-td">
                  <button className="sign-btn" onClick={() => handleSignImage(image.id)}>
                    Sign
                  </button>
                </td>
                <td className="btn-td">
                  <button className="reject-btn" onClick={() => handleAdminRejectImage(image.id)}>
                    Reject
                  </button>
                </td>
                <td className="btn-td">
                  <button className="view-btn" onClick={() => handleViewSupportAdminImage(image.id)}>
                    View
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }
  else if (role == "Support") {
    return (
      <div className="images-container">
        <table className="images-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Status</th>
              <th>Accept</th>
              <th>Reject</th>
              <th>View</th>
            </tr>
          </thead>
          <tbody>
            {images.map((image) => (
              <tr key={image.id}>
                <td>
                  {image.imageName && image.imageName.length > 15
                    ? `${image.imageName.substring(0, 15)}...`
                    : image.imageName || "No Name"}
                </td>
                <td>{image.status}</td>
                <td className="download-td">
                  <button className="accept-btn" onClick={() => handleAcceptImage(image.id)}>
                    Accept
                  </button>
                </td>
                <td className="btn-td">
                  <button className="reject-btn" onClick={() => handleSupportRejectImage(image.id)}>
                    Reject
                  </button>
                </td>
                <td className="btn-td">
                  <button className="view-btn" onClick={() => handleViewSupportAdminImage(image.id)}>
                    View
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }
  return (
    <div className="images-container">
      <table className="images-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Download</th>
            <th>Delete</th>
            <th>View</th>
          </tr>
        </thead>
        <tbody>
          {images.map((image) => (
            <tr key={image.id}>
              <td>
                {image.imageName && image.imageName.length > 15
                  ? `${image.imageName.substring(0, 15)}...`
                  : image.imageName || "No Name"}
              </td>
              <td>{image.status}</td>
              <td className="download-td">
                <button className="download-btn" onClick={() => handleDownloadImage(image.id)}>
                  Download
                </button>
              </td>
              <td className="btn-td">
                <button className="del-btn" onClick={() => handleDeleteImage(image.id)}>
                  Delete
                </button>
              </td>
              <td className="btn-td">
                <button className="view-btn" onClick={() => handleViewImage(image.id)}>
                  View
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default Images;
