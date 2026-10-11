import torch
import matplotlib.pyplot as plt
from torchvision import datasets, transforms
from torch.utils.data import Subset, Dataset, DataLoader


################ dataset subsetting & dataloader utils ################
cifar10_classes = (
    'plane',
    'car',
    'bird',
    'cat',
    'deer',
    'dog',
    'frog',
    'horse',
    'ship',
    'truck',
)


def load_cifar10(root="./data", train=True, download=True, normalize=True):
    t = [transforms.ToTensor()]
    if normalize:
        t.append(transforms.Normalize((0.4914, 0.4822, 0.4465), (0.2023, 0.1994, 0.2010)))
    transform = transforms.Compose(t)
    return datasets.CIFAR10(root=root, train=train, download=download, transform=transform)


def subset_by_classes(dataset, classes):
    """Return a Subset of dataset containing only given classes."""
    idx = [i for i, y in enumerate(dataset.targets) if y in classes]
    return Subset(dataset, idx)


class RemapLabels(Dataset):
    """Wrap a Subset and remap its labels to contiguous indices."""

    def __init__(self, subset, class_map):
        self.subset = subset
        self.class_map = class_map

    def __getitem__(self, idx):
        x, y = self.subset[idx]
        return x, self.class_map[y]

    def __len__(self):
        return len(self.subset)


def get_cifar_subset(classes, root="./data", batch_size=64, normalize=True):
    """Load CIFAR10, subset classes, remap labels, and return DataLoaders."""
    trainset = load_cifar10(root, train=True, download=True, normalize=normalize)
    testset = load_cifar10(root, train=False, download=True, normalize=normalize)

    train_subset = subset_by_classes(trainset, classes)
    test_subset = subset_by_classes(testset, classes)

    class_map = {original: i for i, original in enumerate(classes)}
    train_subset = RemapLabels(train_subset, class_map)
    test_subset = RemapLabels(test_subset, class_map)

    trainloader = DataLoader(train_subset, batch_size=batch_size, shuffle=True)
    testloader = DataLoader(test_subset, batch_size=batch_size, shuffle=False)

    return trainloader, testloader, class_map


################### feature map collection & representation metrics utils ##############

@torch.inference_mode()
def collect_block2_feature_maps(model, dataloader, device, max_batches=None):
    """
    Retourne une liste de feature maps [B,C,H,W] (sorties de block2),
    une entrée par batch du dataloader 
    """
    model = model.to(device)
    model.eval()

    feats = []
    hook_out = {}

    def hook_fn(module, inp, out):
        hook_out["feat"] = out.detach()

    h = model.block2.register_forward_hook(hook_fn)

    for b, (X, y) in enumerate(dataloader):
        X = X.to(device)
        _ = model(X)  # déclenche le hook
        feats.append(hook_out["feat"].cpu())  # [B,C,H,W]

        if max_batches is not None and (b + 1) >= max_batches:
            break

    h.remove()
    return feats



def cosine_similarity_channels_from_feats(feats_list):
    """
    feats_list: liste de tensors [B,C,H,W]
    Calcule une matrice cosine similarity [C,C] entre canaux (agrégée sur batches & pixels).
    """
    S = None  # somme des dot-products [C,C]
    n = None  # somme des normes^2 [C]

    for feat in feats_list:
        if feat.dim() != 4:
            raise ValueError(f"Expected [B,C,H,W], got {feat.shape}")

        B, C, H, W = feat.shape
        F = feat.permute(1, 0, 2, 3).reshape(C, -1).float()  # [C, B*H*W]

        if S is None:
            S = torch.zeros((C, C))
            n = torch.zeros((C,))

        S += F @ F.t()
        n += (F * F).sum(dim=1)

    eps = 1e-8
    denom = torch.sqrt(n.unsqueeze(1) * n.unsqueeze(0)) + eps
    cos = S / denom  # [C,C]

    # stats off-diagonal
    eye = torch.eye(cos.size(0)).bool()
    off = cos[~eye]
    stats = {
        "C": cos.size(0),
        "mean_offdiag": off.mean().item(),
        "mean_abs_offdiag": off.abs().mean().item(),
        "max_abs_offdiag": off.abs().max().item(),
        "frac_abs_gt_0.5": (off.abs() > 0.5).float().mean().item(),
    }
    return cos, stats


def effective_rank_channels_from_feats(feats_list):
    """
    feats_list: liste de tensors [B,C,H,W]
    Calcule le rang effectif de la matrice d'activations [B*H*W,C].
    """
    matrices = []

    for feat in feats_list:
        if feat.dim() != 4:
            raise ValueError(f"Expected [B,C,H,W], got {feat.shape}")

        B, C, H, W = feat.shape
        X = feat.permute(0, 2, 3, 1).reshape(-1, C).float()  # [B*H*W, C]
        matrices.append(X)

    X = torch.cat(matrices, dim=0)
    singular_values = torch.linalg.svdvals(X)
    p = singular_values / (singular_values.sum() + 1e-12)
    entropy = -(p * torch.log(p + 1e-12)).sum()
    erank = torch.exp(entropy)

    stats = {
        "C": X.shape[1],
        "effective_rank": erank.item(),
        "normalized_effective_rank": (erank / X.shape[1]).item(),
        "singular_values": singular_values.tolist(),
    }
    return erank, stats

def plot_cosine_heatmap(cos, title):
    plt.figure()
    plt.imshow(cos, cmap="coolwarm", vmin=-1, vmax=1)
    plt.title(title)
    plt.colorbar()
    plt.xlabel("channel")
    plt.ylabel("channel")
    plt.show()
