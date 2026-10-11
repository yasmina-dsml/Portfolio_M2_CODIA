import matplotlib.pyplot as plt
from sklearn.metrics import ConfusionMatrixDisplay
from torcheval.metrics import (
    MulticlassConfusionMatrix,
    MulticlassPrecision,
    MulticlassRecall,
    MulticlassF1Score,
)
import torch

device = torch.accelerator.current_accelerator().type if torch.accelerator.is_available() else "cpu"


def train_step(dataloader, model, loss_fn, optimizer):
    model.train() #set model to training mode(enables dropout and batchnorm if used)
    loss = 0
    for batch, (X, y) in enumerate(dataloader):
        X, y = X.to(device), y.to(device)
        pred = model(X) #shape [B,n_classes] logits for each class, not probabilities
        loss = loss_fn(pred, y)
        loss.backward() #compute gradients 
        optimizer.step() #update weights
        optimizer.zero_grad() #reset gradients for next batch 
    return loss

@torch.inference_mode() #disables gradient computation ,reduces memory usage and speeds up inference
def eval_step(dataloader, model, loss_fn):
    size = len(dataloader.dataset)
    num_batches = len(dataloader)
    model.eval()
    eval_loss, correct = 0, 0 #loss sum and correct predictions count
    for X, y in dataloader:
        X, y = X.to(device), y.to(device)
        pred = model(X)
        eval_loss += loss_fn(pred, y).item() #add batch loss to total eval loss
        correct += (pred.argmax(dim=1) == y).type(torch.float).sum().item()
    
    eval_loss /= num_batches #mean loss across batches
    acc = 100*correct/size 
    return eval_loss, acc

def train(trainloader, testloader, model, optimizer, loss_fn, n_epochs=10):
    train_loss   = []
    eval_metrics = []

    for t in range(n_epochs):
        train_loss += [train_step(trainloader, model, loss_fn, optimizer).cpu().detach().numpy()]
        eval_metrics += [eval_step(testloader, model, loss_fn)]
        print(f"Epoch {t+1} --- Train loss: {train_loss[t]:>8f} -- Val loss {eval_metrics[t][0]:>8f} -- Val acc.: {eval_metrics[t][1]:>0.1f} ")

    print("Done")
    return train_loss, eval_metrics

def plot_training_dyn(train_loss, eval_metrics):
    epochs = [i+1 for i in range(len(eval_metrics))]
    val_loss = [eval_metrics[i][0] for i in range(len(eval_metrics))]

    plt.plot(epochs, train_loss, label="Train")
    plt.plot(epochs, val_loss, label="Validation")
    plt.xlabel('Epochs')
    plt.ylabel('Loss')
    plt.legend()
    plt.show()

@torch.inference_mode()
def model_evaluation(model, dataloader, num_classes, topk=2):
    model.eval()
    cfm = MulticlassConfusionMatrix(num_classes=num_classes)
    precision = MulticlassPrecision(num_classes=num_classes, average=None)
    recall = MulticlassRecall(num_classes=num_classes, average=None)
    f1 = MulticlassF1Score(num_classes=num_classes, average=None)

    top1_correct = 0
    topk_correct = 0

    for X, y in dataloader:
        X, y = X.to(device), y.to(device)
        logits = model(X)

        # Predictions and top-k predictions
        predictions = torch.argmax(logits, dim=1)
        _, topk_indices = torch.topk(logits, k=topk, dim=1)
        top1_correct += (predictions == y).sum().item()
        topk_correct += ((topk_indices == y.view(-1, 1)).any(dim=1)).sum().item()

        # Update stateful metrics
        cfm.update(predictions, y)
        precision.update(predictions, y)
        recall.update(predictions, y)
        f1.update(predictions, y)

    top1_acc = top1_correct / len(dataloader.dataset)
    topk_acc = topk_correct / len(dataloader.dataset)
    
    return {
        "top1_accuracy": top1_acc,
        "topk_accuracy": topk_acc,#top-3
        "confusion_matrix": cfm.compute(),
        "precision": precision.compute(),
        "recall": recall.compute(),
        "f1": f1.compute()
    }

def print_per_class_metrics(results, class_names=None, model_name="Model"):
    if class_names is None:
        class_names = list(range(len(results["precision"])))
    print(f"{model_name} per-class metrics:")
    for i, class_name in enumerate(class_names):
        print(
            f"  {class_name:<12} "
            f"precision={results['precision'][i].item():.4f} "
            f"recall={results['recall'][i].item():.4f} "
            f"f1={results['f1'][i].item():.4f}"
        )


def plot_cfm(cfm, class_names):
    disp = ConfusionMatrixDisplay(cfm, display_labels=class_names)
    disp.plot(cmap="Blues")
    plt.show()


def per_class_metrics(cfm, class_names=[]):
    metrics = {}
    correct = 0
    total = cfm.sum()
    for c in range(len(cfm)):
        TP = cfm[c, c]
        FP = cfm[:, c].sum() - TP
        FN = cfm[c, :].sum() - TP

        precision = TP / (TP + FP) if (TP + FP) > 0 else 0
        recall = TP / (TP + FN) if (TP + FN) > 0 else 0
        f1 = 2 * precision * recall / (precision + recall) if (precision + recall) > 0 else 0
        correct += TP

        key = c if class_names == [] else class_names[c]

        metrics[key] = {
            'precision': precision,
            'recall': recall,
            'f1': f1
        }
    accuracy = correct / total if total > 0 else 0
    return accuracy, metrics

################################################################################

if __name__ == "__main__":
    print(f"Using {device} device")
